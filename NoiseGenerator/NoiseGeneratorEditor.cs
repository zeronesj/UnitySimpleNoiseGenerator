using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NoiseGenerator
{
    public class NoiseGeneratorEditor : EditorWindow
    {
        [MenuItem("Tool/Noise Generator")]
        public static void OpenWindow()
        {
            EditorWindow.GetWindow<NoiseGeneratorEditor>(false,"Noise Generator");
        }

        #region compute shader
        private ComputeShader _valueNoiseShader;
        private ComputeShader _perlinNoiseShader;
        private ComputeShader _simplexNoiseShader;
        private ComputeShader _worleyPerlinNoiseShader;
        private ComputeShader _worleyNoiseShader;
        private ComputeShader _perlinNoise3DShader;
        private ComputeShader _worleyNoise3DShader;
        private ComputeShader _worleyPerlinNoise3DShader;
        #endregion
        
        
        
        private TextureSize _selectSize = TextureSize._256;
        private int _selectMode=0;
        private int _selectChanel = 0;
        
        private bool _EnableMultiChanel;
        private NoiseType[] _selectNoise = new NoiseType[4]
        {
            NoiseType.ValueNoise,NoiseType.ValueNoise,
            NoiseType.ValueNoise,NoiseType.ValueNoise
        };
        private Noise3DType[] _select3DNoise = new Noise3DType[4]
        {
            Noise3DType.PerlinNoise,Noise3DType.PerlinNoise,
            Noise3DType.PerlinNoise,Noise3DType.PerlinNoise
        };
        private FbmType[] _selectFbm =new FbmType[4]
        {
            FbmType.dontuse,FbmType.dontuse,
            FbmType.dontuse,FbmType.dontuse
        };
        private float[] _noiseScale=new float[4]
        {
            5,5,5,5
        };

        #region fbm相关的属性
        
        //Properties
        int[] _octaves =new int[]{6,6,6,6}; 
        float[] _lacunarity = new float[4]{2,2,2,2};
        float[] _gain =new float[4]{0.5f,0.5f,0.5f,0.5f};
        // Initial values
        float[] _amplitude =new float[4]{0.5f,0.5f,0.5f,0.5f};
        float[] _frequency =new float[4]{1.0f,1.0f,1.0f,1.0f}; 

        #endregion
        private string _savePath;
        private string _save3DPath="Texture3D";
        private void OnEnable()
        {
            _valueNoiseShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/ValueNoise.compute");
            _perlinNoiseShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/PerlinNoise.compute");
            _simplexNoiseShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/SimplexNoise.compute");
            _worleyNoiseShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/WorleyNoise.compute");
            _worleyPerlinNoiseShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/Worley_PerlinNoise.compute");
            
            _perlinNoise3DShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/PerlinNoise3D.compute");
            _worleyNoise3DShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/WorleyNoise3D.compute");
            _worleyPerlinNoise3DShader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/Editor/NoiseGenerator/Worley_PerlinNoise3D.compute");
        }

        private void OnDisable()
        {
            Resources.UnloadAsset(_valueNoiseShader);
        }

        private void OnGUI()
        {
            _selectMode=GUILayout.Toolbar(_selectMode, new[] {"2D", "3D"});
            
            if (_selectMode==0)
            {//2D界面
                EditorGUILayout.LabelField("Texture Setting",new GUIStyle(){fontStyle = FontStyle.Bold});
                _selectSize = (TextureSize)EditorGUILayout.EnumPopup("Texture Size", _selectSize);
                EditorGUILayout.LabelField("Noise Setting",new GUIStyle(){fontStyle = FontStyle.Bold});
                _EnableMultiChanel=EditorGUILayout.Toggle("multi chanel",_EnableMultiChanel);
                int ChanelIndex = 0;
                if (_EnableMultiChanel)
                {
                    _selectChanel=GUILayout.Toolbar(_selectChanel, new[] {"R", "G", "B", "A"});
                    ChanelIndex = _selectChanel;
                }
                _noiseScale[ChanelIndex] = EditorGUILayout.Slider("Noise Scale", _noiseScale[ChanelIndex], 1, 10);
                _selectNoise[ChanelIndex] = (NoiseType) EditorGUILayout.EnumPopup("Noise Type", _selectNoise[ChanelIndex]);
                _selectFbm[ChanelIndex] = (FbmType) EditorGUILayout.EnumPopup("FBM Type", _selectFbm[ChanelIndex]);
                if (_selectFbm[ChanelIndex]!= FbmType.dontuse)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Properties",EditorStyles.boldLabel);
                    _octaves[ChanelIndex] = EditorGUILayout.IntField("Octaves", _octaves[ChanelIndex]);
                    _lacunarity[ChanelIndex] = EditorGUILayout.FloatField("lacunarity", _lacunarity[ChanelIndex]);
                    _gain[ChanelIndex] = EditorGUILayout.Slider("gain", _gain[ChanelIndex], 0, 1);
                    EditorGUILayout.LabelField("Init Value",EditorStyles.boldLabel);
                    _amplitude[ChanelIndex] = EditorGUILayout.FloatField("amplitude", _amplitude[ChanelIndex]);
                    _frequency[ChanelIndex] = EditorGUILayout.FloatField("frequency", _frequency[ChanelIndex]);
                    EditorGUI.indentLevel--;
                }
                if (GUILayout.Button("Generate"))
                {
                    if (!_EnableMultiChanel)
                    { 
                        GenNoise2D();
                    }
                    else
                    {
                        GenNoise2DMulti();
                    } 
                }

                if (_perview2DImage[ChanelIndex]!=null)
                {
                    GUILayout.Box(_perview2DImage[ChanelIndex],new []{GUILayout.Height(_perview2DImage[ChanelIndex].height),GUILayout.Width(_perview2DImage[ChanelIndex].width)});
                }
                if (GUILayout.Button("Save"))
                {
                    _savePath = "";
                    _savePath=EditorUtility.SaveFilePanel("保存贴图", Application.dataPath, "noise", "png");
                    if (_savePath!="")
                    {
                        SaveNoise2D();
                    }
                }
            }

            if (_selectMode==1)
            {//3D贴图
                EditorGUILayout.LabelField("Texture Setting",new GUIStyle(){fontStyle = FontStyle.Bold});
                _selectSize = (TextureSize)EditorGUILayout.EnumPopup("Texture Size", TextureSize._128);
                EditorGUILayout.HelpBox("only support 128x128", MessageType.Info);
                EditorGUILayout.LabelField("Noise Setting",new GUIStyle(){fontStyle = FontStyle.Bold});
                _EnableMultiChanel=EditorGUILayout.Toggle("multi chanel",_EnableMultiChanel);
                int ChanelIndex = 0;
                if (_EnableMultiChanel)
                {
                    _selectChanel=GUILayout.Toolbar(_selectChanel, new[] {"R", "G", "B", "A"});
                    ChanelIndex = _selectChanel;
                }
                _noiseScale[ChanelIndex] = EditorGUILayout.Slider("Noise Scale", _noiseScale[ChanelIndex], 1, 10);
                _select3DNoise[ChanelIndex] = (Noise3DType) EditorGUILayout.EnumPopup("Noise Type", _select3DNoise[ChanelIndex]);
                _selectFbm[ChanelIndex] = (FbmType) EditorGUILayout.EnumPopup("FBM Type", _selectFbm[ChanelIndex]);
                if (_selectFbm[ChanelIndex]!= FbmType.dontuse)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("Properties",EditorStyles.boldLabel);
                    _octaves[ChanelIndex] = EditorGUILayout.IntField("Octaves", _octaves[ChanelIndex]);
                    _lacunarity[ChanelIndex] = EditorGUILayout.FloatField("lacunarity", _lacunarity[ChanelIndex]);
                    _gain[ChanelIndex] = EditorGUILayout.Slider("gain", _gain[ChanelIndex], 0, 1);
                    EditorGUILayout.LabelField("Init Value",EditorStyles.boldLabel);
                    _amplitude[ChanelIndex] = EditorGUILayout.FloatField("amplitude", _amplitude[ChanelIndex]);
                    _frequency[ChanelIndex] = EditorGUILayout.FloatField("frequency", _frequency[ChanelIndex]);
                    EditorGUI.indentLevel--;
                }
                
                if (GUILayout.Button("Generate"))
                {
                    if (_save3DPath!="")
                    {
                        if (!_EnableMultiChanel)
                        { 
                            GenNoise3D();
                        }
                        else
                        {
                            GenNoise3DMulti();
                        }
                    }
                }
                
                if (_perview3DImage[ChanelIndex]!=null)
                {
                    GUILayout.Box(_perview3DImage[ChanelIndex],new []{GUILayout.Height(_perview3DImage[ChanelIndex].height),GUILayout.Width(_perview3DImage[ChanelIndex].width)});
                }
                
                _save3DPath = EditorGUILayout.TextField("3D Texture Name", _save3DPath);
                
                if (GUILayout.Button("Save"))
                {
                    if (_savePath!="")
                    {
                        SaveNoise3D();
                    }
                }

            }
            

            
            
            
            

        }

        #region 2DTexture

        private Texture2D[] _perview2DImage = new Texture2D[4];
        float[][] _noise2D=new float[4][];
        float[] Get2DNoiseValue(int chanel)
        {
            ComputeShader shader;
            switch (_selectNoise[chanel])
            {
                case NoiseType.ValueNoise:
                    shader=_valueNoiseShader;
                    break;
                case NoiseType.PerlinNoise:
                    shader=_perlinNoiseShader;
                    break;
                case NoiseType.SimplexNoise:
                    shader=_simplexNoiseShader;
                    break;
                case NoiseType.WorleyNoise:
                    shader=_worleyNoiseShader;
                    break;
                case NoiseType.Perlin_WorleyNoise:
                    shader = _worleyPerlinNoiseShader;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            TextureSize TexSize = _selectSize;
            FbmType fbm = _selectFbm[chanel];
            int size;
            switch (TexSize)
            {
                case TextureSize._128: size = 128;break;
                case TextureSize._256: size = 256;break;
                case TextureSize._512: size = 512;break;
                default: size = 256;break;
            }
            float[] noise=new float[size*size];
            ComputeBuffer cb1=new ComputeBuffer(noise.Length,4);
            cb1.SetData(noise);
            shader.SetFloat("TextureSize",size);
            shader.SetFloat("NoiseScale",2*_noiseScale[chanel]);
            if (fbm!= FbmType.dontuse)
            {
                SetFbmProperties(shader,chanel);
            }
            int threadGroupsX = Mathf.CeilToInt(size / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(size / 8.0f);
            switch (fbm)
            {
                case FbmType.dontuse:
                    shader.SetBuffer(0,"Result",cb1);
                    shader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
                    break;
                case FbmType.standard:
                    shader.SetBuffer(1,"Result",cb1);
                    shader.Dispatch(1, threadGroupsX, threadGroupsY, 1);
                    break;
                case FbmType.turbulence:
                    shader.SetBuffer(2,"Result",cb1);
                    shader.Dispatch(2, threadGroupsX, threadGroupsY, 1);
                    break;
                case FbmType.ridge:
                    shader.SetBuffer(3,"Result",cb1);
                    shader.Dispatch(3, threadGroupsX, threadGroupsY, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fbm", fbm, null);
            }
            cb1.GetData(noise);
            cb1.Release();
            Update2DPerview(size,chanel,noise);
            return noise;
        }
        void Update2DPerview(int size,int chanel,float[] noise)
        {
            Color[] colors=new Color[noise.Length];
            for (int i = 0; i < noise.Length; i++)
            {
                var n = noise[i];
                colors[i]=new Color(n,n,n,1.0f);
            }
            //转换成颜色
            _perview2DImage[chanel] = new Texture2D (size, size, TextureFormat.ARGB32, true);
            _perview2DImage[chanel].SetPixels(colors);
            _perview2DImage[chanel].Apply();
        }
        void GenNoise2D()
        {//单通道默认全使用第一个通道的值
            _noise2D[0] = Get2DNoiseValue(0);
            
        }
        void GenNoise2DMulti()
        {
            _noise2D[0] = Get2DNoiseValue(0);
            _noise2D[1] = Get2DNoiseValue(1);
            _noise2D[2] = Get2DNoiseValue(2);
            _noise2D[3] = Get2DNoiseValue(3);
            
        }
        void SaveNoise2D()
        {
            if (!_EnableMultiChanel&&_noise2D[0]==null)
            {
                Debug.LogWarning("请生成2D贴图后再保存");
                return;
            }
            if(_EnableMultiChanel&&_noise2D[1]==null)
            {
                Debug.LogWarning("2D贴图的其余通道尚未生成");
                return;
            }
            int size;
            switch (_selectSize)
            {
                case TextureSize._128: size = 128;break;
                case TextureSize._256: size = 256;break;
                case TextureSize._512: size = 512;break;
                default: size = 256;break;
            }
            Color[] colors=new Color[_noise2D[0].Length];
            for (int i = 0; i < colors.Length; i++)
            {
                if (_EnableMultiChanel)
                {
                    colors[i]=new Color(_noise2D[0][i],_noise2D[1][i],_noise2D[2][i],_noise2D[3][i]);
                }
                else
                {
                    colors[i]=new Color(_noise2D[0][i],_noise2D[0][i],_noise2D[0][i],_noise2D[0][i]);

                }
            }
            //转换成颜色
            var tex = new Texture2D (size, size, TextureFormat.ARGB32, true);
            tex.SetPixels(colors);
            tex.Apply();
            File.WriteAllBytes( _savePath,tex.EncodeToPNG());
            AssetDatabase.Refresh();
        }

        #endregion
        
        private Texture2D[] _perview3DImage = new Texture2D[4];
        float[][] _noise3D=new float[4][];
        float[] Get3DNoiseValue(int chanel)
        {
            ComputeShader shader;
            switch (_select3DNoise[chanel])
            {
                case Noise3DType.PerlinNoise:
                    shader=_perlinNoise3DShader;
                    break;
                case Noise3DType.WorleyNoise:
                    shader=_worleyNoise3DShader;
                    break;
                case Noise3DType.Perlin_WorleyNoise:
                    shader = _worleyPerlinNoise3DShader;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            FbmType fbm = _selectFbm[chanel];
            int size = 128;
            float[] noise=new float[size*size*size];
            ComputeBuffer cb1=new ComputeBuffer(noise.Length,4);
            cb1.SetData(noise);
            shader.SetFloat("TextureSize",size);
            shader.SetFloat("NoiseScale",2*_noiseScale[chanel]);
            if (fbm!= FbmType.dontuse)
            {
                SetFbmProperties(shader,chanel);
            }
            int threadGroupsX = Mathf.CeilToInt(size / 8.0f);
            int threadGroupsY = Mathf.CeilToInt(size / 8.0f);
            int threadGroupsZ = Mathf.CeilToInt(size / 8.0f);
            switch (fbm)
            {
                case FbmType.dontuse:
                    shader.SetBuffer(0,"Result",cb1);
                    shader.Dispatch(0, threadGroupsX, threadGroupsY, threadGroupsZ);
                    break;
                case FbmType.standard:
                    shader.SetBuffer(1,"Result",cb1);
                    shader.Dispatch(1, threadGroupsX, threadGroupsY, threadGroupsZ);
                    break;
                case FbmType.turbulence:
                    shader.SetBuffer(2,"Result",cb1);
                    shader.Dispatch(2, threadGroupsX, threadGroupsY, threadGroupsZ);
                    break;
                case FbmType.ridge:
                    shader.SetBuffer(3,"Result",cb1);
                    shader.Dispatch(3, threadGroupsX, threadGroupsY, threadGroupsZ);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("fbm", fbm, null);
            }
            cb1.GetData(noise);
            cb1.Release();
            Update3DPerview(size,chanel,noise);
            return noise;
        }
        void Update3DPerview(int size,int chanel,float[] noise)
        {
            //现实2D图片
            Color[] colors=new Color[size*size];
            for (int i = 0; i < colors.Length; i++)
            {
                var n = noise[i];
                colors[i]=new Color(n,n,n,1.0f);
            }
            //转换成颜色
            _perview3DImage[chanel] = new Texture2D (size, size, TextureFormat.ARGB32, true);
            _perview3DImage[chanel].SetPixels(colors);
            _perview3DImage[chanel].Apply();
        }
        void GenNoise3D()
        {
            _noise3D[0] = Get3DNoiseValue(0);
            
        }

        void GenNoise3DMulti()
        {
            
            _noise3D[0] = Get3DNoiseValue(0);
            _noise3D[1] = Get3DNoiseValue(1);
            _noise3D[2] = Get3DNoiseValue(2);
            _noise3D[3] = Get3DNoiseValue(3);
        }

        void SaveNoise3D()
        {    
            if (!_EnableMultiChanel&&_noise3D[0]==null)
            {
                Debug.LogWarning("请生成3D贴图后再保存");
                return;
            }
            if(_EnableMultiChanel&&_noise3D[1]==null)
            {
                Debug.LogWarning("3D贴图的其余通道尚未生成");
                return;
            }
            int size = 128;
            //保存贴图
            var tex = new Texture3D (size, size,size, TextureFormat.ARGB32, true);
            var colors=new Color[size*size*size];
            
            for (int i = 0; i < colors.Length; i++)
            {
                if (_EnableMultiChanel)
                {
                    float r = _noise3D[0][i];
                    float g = _noise3D[1][i];
                    float b = _noise3D[2][i];
                    float a = _noise3D[3][i];
                    colors[i]=new Color(r,g,b,a);
                }
                else
                {
                    float n = _noise3D[0][i];
                    colors[i]=new Color(n,n,n,n);
                }
                
            }
            tex.SetPixels(colors);
            tex.Apply();
            AssetDatabase.CreateAsset(tex, "Assets/"+_save3DPath+".asset");
            AssetDatabase.Refresh();
        }
        
        
        void SetFbmProperties(ComputeShader shader,int index)
        { 
            shader.SetFloat("NoiseScale",2*_noiseScale[index]);
            shader.SetInt("_Octaves",_octaves[index]);
            shader.SetFloat("_Lacunarity",_lacunarity[index]);
            shader.SetFloat("_Gain",_gain[index]);
            shader.SetFloat("_Amplitude",_amplitude[index]);
            shader.SetFloat("_Frequency",_frequency[index]);
        } 
    }

    public enum TextureSize
    {
        _128,
        _256,
        _512
    }
    public enum NoiseType
    {
        ValueNoise,
        PerlinNoise,
        SimplexNoise,
        WorleyNoise,
        Perlin_WorleyNoise
        
    }
    public enum Noise3DType
    {
        PerlinNoise,
        WorleyNoise,
        Perlin_WorleyNoise
    }
    public enum FbmType
    {
        dontuse,
        standard,
        turbulence,
        ridge
    }

}

