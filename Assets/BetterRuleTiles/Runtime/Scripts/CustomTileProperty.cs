using UnityEngine;

namespace VinTools.BetterRuleTiles.Internal
{
    [System.Serializable]
    public class CustomTileProperty
    {
        public string _key;
        public Type _type;

        public int _val_int;
        public float _val_float;
        public double _val_double;
        public char _val_char;
        public string _val_string;
        public bool _val_bool;
        //public UnityEngine.Object _val_obj;

        public enum Type
        {
            Int = 0,
            Float = 1,
            Double = 2,
            Char = 3,
            String = 4,
            Bool = 5,
            //Object = 6,
        }


        public CustomTileProperty() { }
        public CustomTileProperty(string key)
        {
            _key = key;
        }
        public CustomTileProperty(CustomTileProperty original)
        {
            _key = original._key;
            _type = original._type;

            switch (original._type)
            {
                case Type.Int:
                    _val_int = original._val_int;
                    break;
                case Type.Float:
                    _val_float = original._val_float;
                    break;
                case Type.Double:
                    _val_double = original._val_double;
                    break;
                case Type.Char:
                    _val_char = original._val_char;
                    break;
                case Type.String:
                    _val_string = original._val_string;
                    break;
                case Type.Bool:
                    _val_bool = original._val_bool;
                    break;
                //case Type.Object:
                //    _val_obj = original._val_obj;
                //    break;
                default:
                    break;
            }
        }

        public void SetInt(int val)
        {
            _type = Type.Int;
            _val_int = val;
        }
        public void SetFloat(float val)
        {
            _type = Type.Float;
            _val_float = val;
        }
        public void SetDouble(double val)
        {
            _type = Type.Double;
            _val_double = val;
        }
        public void SetChar(char val)
        {
            _type = Type.Char;
            _val_char = val;
        }
        public void SetString(string val)
        {
            _type = Type.String;
            _val_string = val;
        }
        public void SetBool(bool val)
        {
            _type = Type.Bool;
            _val_bool = val;
        }
        //public void SetObject(UnityEngine.Object val)
        //{
        //    _type = Type.Object;
        //    _val_obj = val;
        //}

        public int GetInt()
        {
            if (_type != Type.Int) ShowTypeError(Type.Int);
            return _val_int;
        }
        public float GetFloat()
        {
            if (_type != Type.Float) ShowTypeError(Type.Float);
            return _val_float;
        }
        public double GetDouble()
        {
            if (_type != Type.Double) ShowTypeError(Type.Double);
            return _val_double;
        }
        public char GetChar()
        {
            if (_type != Type.Char) ShowTypeError(Type.Char);
            return _val_char;
        }
        public string GetString()
        {
            if (_type != Type.String) ShowTypeError(Type.String);
            return _val_string;
        }
        public bool GetBool()
        {
            if (_type != Type.Bool) ShowTypeError(Type.Bool);
            return _val_bool;
        }
        //public UnityEngine.Object GetObject()
        //{
        //    if (_type != Type.Object) ShowTypeError(Type.Object);
        //    return _val_obj;
        //}

        private void ShowTypeError(Type requested) => Debug.LogWarning($"The type you're requesting ({requested}) is not the same as the type that was set for the property ({_type}).");
    }
}