using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using NLua;
using fastJSON;

namespace calc_from_geometryOfMotor
{
    public class Parameter
    {
        private static readonly ILog log = LogManager.GetLogger("ParamCol");

        public String group { get; set; }//0//type name of an object like: Motor\Rotor
        public String name { get; set; }//1//properties name (in that object)
        public String text { get; set; }//text at input (lua statement)//2
        public object value { get; set; }// actual value//3 (string, need to be converted)

        [JsonIgnore]
        public String desc { get; set; }//4
        [JsonIgnore]
        public String status { get; set; }//5
        [JsonIgnore]
        public Type valueType { get; set; }//6

        /// <summary>
        /// Value type as string
        /// </summary>
        public String valueTypeString
        {
            get
            {
                return valueType.ToString();
            }
            set
            {
                valueType = Type.GetType(value);
            }
        }

        public String fullname
        {
            get
            {
                return group + '.' + name;
            }
        }

        public static readonly int INDEX_GROUP = 0;
        public static readonly int INDEX_NAME = 1;
        public static readonly int INDEX_TEXT = 2;
        public static readonly int INDEX_VALUE = 3;
        public static readonly int INDEX_DESC = 4;
        public static readonly int INDEX_STATUS = 5;
        public static readonly int INDEX_VALUETYPE = 6;

        public void EvaluateValue()
        {
            // do evaluation of the param (using lua)
            Lua lua_state = LuaHelper.GetLuaState();
            try
            {
                if (valueType.Equals(typeof(double)))
                    value = (double)lua_state.DoString("return " + text)[0];
                else if (valueType.Equals(typeof(int)))
                    value = int.Parse(lua_state.DoString("return " + text)[0].ToString());
                else if (valueType.IsEnum)
                    value = Enum.Parse(valueType, text);
                else if (valueType.Equals(typeof(bool)))
                    value = bool.Parse(text);
                else if (valueType.Equals(typeof(string)))
                    value = text;
            }
            catch (Exception ex)
            {
                log.Error("Error evaluating value of " + name + ": " + ex.Message);
                return;
            }
        }
    }

    public class ParametersCollection : List<Parameter>
    {
        private static readonly ILog log = LogManager.GetLogger("ParamCol");

        public ParametersCollection()
            : base()
        {

        }

        public ParametersCollection(IEnumerable<Parameter> List)
            : base(List)
        {
        }

        /// <summary>
        /// Build a parameter collection from all properties and their values to a list of parameters
        /// </summary>
        /// <param name="groupName">Group name for this object</param>
        /// <param name="obj"></param>
        /// <param name="_public_param_">true if get public set-able-properties (input), 
        /// false if get non-setable-public properties (output)</param>
        /// <returns></returns>
        public static ParametersCollection FromObject(String groupName, object obj)
        {
            if (obj == null)
                return null;

            ParametersCollection pc = new ParametersCollection();

            Type type = obj.GetType();

            PropertyInfo[] properties = type.GetProperties();
            foreach (PropertyInfo pi in properties)
            {
                if (pi.GetSetMethod() == null)
                    continue;

                // if exclude in params, then ignore
                bool exludeThisProperty = false;
                foreach (ExcludeFromParamsAttribute a in pi.GetCustomAttributes(typeof(ExcludeFromParamsAttribute), false))
                {
                    exludeThisProperty = true;
                }
                if (exludeThisProperty)
                    continue;

                // new one
                Parameter ppp = new Parameter();
                ppp.group = groupName;
                ppp.name = pi.Name;
                object value = pi.GetValue(obj, null);
                if (value != null)
                {
                    ppp.text = value.ToString();
                    ppp.value = value;
                }
                foreach (ParamInfoAttribute a in pi.GetCustomAttributes(typeof(ParamInfoAttribute), false))
                {
                    ppp.desc = a.HelpText;
                }

                ppp.valueType = pi.PropertyType;
                ppp.status = "-";
                pc.Add(ppp);

            }

            return pc;
        }

        /// <summary>
        /// Set properties of obj with the params values
        /// </summary>
        /// <param name="groupName">Only param with groupName will be set value</param>
        /// <param name="obj"></param>
        public void putValuesToObject(String groupName, object obj)
        {
            Type type = obj.GetType();

            if (groupName == null || groupName == "")
                groupName = type.Name;

            PropertyInfo[] properties = type.GetProperties();
            // check all properties of type
            foreach (PropertyInfo pi in properties)
            {
                if (pi.GetSetMethod() != null)//only public properties
                {
                    // find that properties in listParamInput
                    foreach (Parameter paraminput in this)
                    {
                        //if match (name, group, valueType)
                        if (paraminput.name == pi.Name && paraminput.group == groupName && paraminput.valueType == pi.PropertyType)
                        {
                            try
                            {
                                pi.SetValue(obj, paraminput.value, null);

                                // This param is parsed successfully
                                paraminput.status = "OK";
                            }
                            catch (Exception e)
                            {
                                // Param is invalid somehow
                                if (e is ArgumentException || e is ArgumentNullException || e is FormatException)
                                {
                                    paraminput.status = paraminput.name + " is invalid";
                                    log.Error(paraminput.group + "\\" + paraminput.status);
                                }
                                else throw;
                            }
                            break;
                        }
                    }
                }
            }
        }

        public Parameter FindParameter(String group, String name)
        {
            foreach (Parameter p in this)
                if (p.group == group && p.name == name)
                    return p;

            return null;
        }

        public String ToJSON()
        {
            return JSON.ToJSON(this);
        }

        public static ParametersCollection FromJSON(String str)
        {
            Parameter[] list = JSON.ToObject<Parameter[]>(str);
            ParametersCollection pc = new ParametersCollection();
            pc.AddRange(list);
            foreach (Parameter p in pc)
                if (p.value != null)
                    if (p.value.GetType() != p.valueType) // p.value can be Jsonxxx type
                        p.value = JSON.ToObject(p.value.ToString(), p.valueType);

            return pc;
        }
    }
}
