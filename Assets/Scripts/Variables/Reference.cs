using Sirenix.OdinInspector;
using UnityEngine;

namespace Variables
{
    [System.Serializable]
    [InlineProperty]
    public abstract class Reference<T, TVariable> where TVariable : ScriptableObject
    {
        [HorizontalGroup("Split")]
        [HideLabel, EnumToggleButtons]
        public ReferenceMode Mode = ReferenceMode.Constant;

        [HorizontalGroup("Split"), HideLabel]
        [ShowIf(nameof(Mode), ReferenceMode.Constant)]
        public T ConstantValue;

        [HorizontalGroup("Split"), HideLabel, ShowIf(nameof(Mode), ReferenceMode.Variable)]
        [InlineEditor]
        public TVariable Variable;

        // Read-only preview of the runtime value when using a variable.
        [ShowInInspector, ReadOnly, HorizontalGroup("Split2")]
        [ShowIf(nameof(Mode), ReferenceMode.Variable), LabelText("")] 
        private T VariableValuePreview => GetVariableValue();

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
}
