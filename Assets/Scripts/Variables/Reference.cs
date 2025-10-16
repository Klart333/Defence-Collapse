using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Variables
{
    [System.Serializable]
    [InlineProperty]
    public abstract class Reference<T, TVariable> where TVariable : ScriptableObject
    {
        public ReferenceMode Mode = ReferenceMode.Constant;

        public T ConstantValue;

        public TVariable Variable;

        public T VariableValuePreview;

        public T Value => Mode == ReferenceMode.Constant ? ConstantValue : GetVariableValue();

        protected abstract T GetVariableValue();
    }
    
    public enum ReferenceMode
    {
        Constant,
        Variable
    }

    [System.Serializable]
    public class FloatReference : Reference<float, FloatVariable>
    {
        protected override float GetVariableValue()
        {
            return Variable != null ? Variable.Value : 0f;
        }
    }
    
    [System.Serializable]
    public class SpriteReference : Reference<Sprite, SpriteVariable>
    {
        protected override Sprite GetVariableValue()
        {
            return Variable != null ? Variable.Value : null;
        }
    }
    
    [System.Serializable]
    public class ColorReference : Reference<Color, ColorVariable>
    {
        public ColorReference(Color constantValue)
        {
            ConstantValue = constantValue;
        }
        
        protected override Color GetVariableValue()
        {
            return Variable != null ? Variable.Value : Color.black;
        }
    }
    
    [System.Serializable]
    public class FontReference : Reference<TMP_FontAsset, FontVariable>
    {
        protected override TMP_FontAsset GetVariableValue()
        {
            return Variable != null ? Variable.Value : null;
        }
    }
    
    [System.Serializable]
    public class StringReference : Reference<string, StringVariable>
    {
        public StringReference(string constantValue)
        {
            ConstantValue = constantValue;
        }
        
        protected override string GetVariableValue()
        {
            return Variable != null ? Variable.Value : "";
        }
    }
    
    [System.Serializable]
    public class TextureReference : Reference<Texture2D, TextureVariable>
    {
        public TextureReference(Texture2D constantValue)
        {
            ConstantValue = constantValue;
        }
        
        protected override Texture2D GetVariableValue()
        {
            return Variable != null ? Variable.Value : null;
        }
    }
}
