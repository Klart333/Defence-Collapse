using Sirenix.OdinInspector;
using UnityEngine;

namespace Variables
{
    [CreateAssetMenu(fileName = "New Float Variable", menuName = "Variable/Float", order = 0)]
    public class FloatVariable : ScriptableObject
    {
        [SerializeField]
        private float value;
        
        [SerializeField]
        private FloatReference variable;
        
        [SerializeField]
        private SpriteReference variable1;

        public float Value => value; 
    }
}