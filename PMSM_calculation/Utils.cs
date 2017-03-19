/****
 * PMSM calculation - automate PMSM design process
 * Created 2016 by Ngo Phuong Le
 * https://github.com/lehn85/pmsm_calculation
 * All files are provided under the MIT license.
 ****/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Reflection;
using ZedGraph;
using System.Collections;
using MathNet.Numerics.LinearAlgebra.Double;
using fastJSON;

namespace calc_from_geometryOfMotor
{
    public class Utils
    {
        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Create a path with name and extension specified
        /// </summary>
        /// <param name="original"></param>
        /// <param name="ext"></param>
        /// <returns></returns>
        public static string GenPathfile(string original, string ext)
        {
            if (!ext.StartsWith("."))
                ext = "." + ext;
            return Path.GetDirectoryName(original) + "\\" + Path.GetFileNameWithoutExtension(original) + ext;
        }

        public static String CalculateObjectMD5Hash(object obj)
        {
            JSONParameters p = new JSONParameters();
            p.UseExtensions = false;
            p.UseFastGuid = false;
            p.UseEscapedUnicode = false;
            p.SerializeNullValues = false;
            p.KVStyleStringDictionary = false;            
            p.IgnoreAttributes = new List<Type>();
            p.IgnoreAttributes.Add(typeof(JsonIgnoreAttribute));
            p.IgnoreAttributes.Add(typeof(ExcludeFromMD5Calculation));

            return CalculateMD5Hash(JSON.ToJSON(obj, p));
        }

        //public static object GetJsonObject(object inputObj)
        //{
        //    //object jo = GetJsonObject(inputObj, false);
        //    //object obj = JsonConvert.Import(jo.ToString());
        //    return inputObj;
        //}

        //private static object GetJsonObject(object inputObj, bool for_md5 = false)
        //{
        //    if (inputObj == null)
        //        return null;
        //    Type type = inputObj.GetType();
        //    // check if type is primitive
        //    if (isPrimitiveType(type))
        //        return inputObj;

        //    // json object for this object
        //    JsonObject jo = new JsonObject();

        //    // check if type is dictionary
        //    if (isDictionaryType(type))
        //    {
        //        Type keyType = type.GetGenericArguments()[0];
        //        Type valueType = type.GetGenericArguments()[1];
        //        IDictionary dict = inputObj as IDictionary;
        //        foreach (object key in dict.Keys)
        //            jo[key.ToString()] = GetJsonObject(dict[key]);

        //        return jo;
        //    }

        //    List<object> members = new List<object>();
        //    members.AddRange(type.GetProperties());
        //    members.AddRange(type.GetFields());

        //    foreach (object objmember in members)
        //    {
        //        // prepare member 
        //        String memberName = "";
        //        Type memberType = null;
        //        object memberValue = null;
        //        Object[] customAttrs = null;
        //        PropertyInfo pi = null;
        //        FieldInfo fi = null;

        //        // if it is property
        //        if (objmember is PropertyInfo)
        //        {
        //            pi = (PropertyInfo)objmember;

        //            // check if has get method, public. If not, continue
        //            if (pi.GetGetMethod() == null)
        //                continue;
        //            // check if has set method, public or non-public. If not, continue
        //            if (!pi.CanWrite)
        //                continue;
        //            // if for md5, but don't have public set method then continue
        //            if (for_md5 && pi.GetSetMethod() == null)
        //                continue;
        //            // also ignore indexer
        //            var indexParams = pi.GetIndexParameters();
        //            if (indexParams != null && indexParams.Length > 0)
        //                continue;

        //            memberName = pi.Name;
        //            memberType = pi.PropertyType;
        //            memberValue = pi.GetValue(inputObj, null);
        //            customAttrs = pi.GetCustomAttributes(false);
        //        }
        //        else // if it is field
        //        {
        //            fi = (FieldInfo)objmember;

        //            if (!fi.IsPublic)
        //                continue;

        //            memberName = fi.Name;
        //            memberType = fi.FieldType;
        //            memberValue = fi.GetValue(inputObj);
        //            customAttrs = fi.GetCustomAttributes(false);
        //        }

        //        // check custom attrs
        //        bool excludeinmd5calc = false;
        //        bool jsonignore = false;
        //        foreach (Attribute a in customAttrs)
        //        {
        //            if (a is ExcludeInMD5Calculation && for_md5)
        //            {
        //                excludeinmd5calc = true;
        //                break;
        //            }
        //            else if (a is JsonIgnoreAttribute)
        //            {
        //                jsonignore = true;
        //                break;
        //            }
        //        }

        //        // if json ignore then ignore this property
        //        if (jsonignore)
        //            continue;
        //        // if get json object to calculate md5 hash and this mark as md5 ignore then continue
        //        if (excludeinmd5calc)
        //            continue;

        //        // if value is null, then ignore
        //        if (memberValue == null)
        //            continue;

        //        //// Here the main part, put value into json
        //        if (isPrimitiveType(memberType))
        //            jo[memberName] = memberValue;
        //        else if (memberType.IsArray)
        //        {
        //            Array objects = memberValue as Array;
        //            bool elementIsPrimitive = isPrimitiveType(memberType.GetElementType());
        //            JsonArray ja = new JsonArray();
        //            foreach (var obj in objects)
        //            {
        //                if (elementIsPrimitive)
        //                    ja.Add(obj);
        //                else ja.Add(GetJsonObject(obj));//if not primitive, go deeper
        //            }
        //            jo.Put(memberName, ja);
        //        }
        //        else // go deeper
        //        {
        //            jo.Put(memberName, GetJsonObject(memberValue));
        //        }
        //        // check if need to go deeper                
        //    }

        //    return jo;
        //}

        private static bool isPrimitiveType(Type p)
        {
            return (p.IsPrimitive || p.IsEnum || p.Equals(typeof(string)));
        }

        private static bool isDictionaryType(Type p)
        {
            return p.IsGenericType && p.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }
    }

    [Serializable]
    public struct PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointF ToPointF()
        {
            return new PointF((float)X, (float)Y);
        }
    }

    public class ListPointD : List<PointD>
    {
        public ListPointD(double[] x, double[] y)
        {
            if (x == null || y == null)
                return;
            int n = (x.Length > y.Length) ? y.Length : x.Length;
            AddRange(Enumerable.Range(0, n).Select(i => new PointD { X = x[i], Y = y[i] }));
        }

        public ListPointD(PointD[] ps)
        {
            if (ps == null)
                return;

            AddRange(Enumerable.Range(0, ps.Length).Select(i => new PointD { X = ps[i].X, Y = ps[i].Y }));
        }

        public ListPointD(int size)
        {
            AddRange(Enumerable.Range(0, size).Select(i => new PointD { X = 0, Y = 0 }));
        }

        public ListPointD()
        {
        }

        public PointPairList ToZedGraphPointPairList()
        {
            PointPairList ppl = new PointPairList(this.Select(p => p.X).ToArray(), this.Select(p => p.Y).ToArray());
            return ppl;
        }
    }

    public class Vector2D : DenseVector
    {
        public double X
        {
            get { return base[0]; }
            set { base[0] = value; }
        }

        public double Y
        {
            get { return base[1]; }
            set { base[1] = value; }
        }

        public Vector2D() : this(0, 0)
        {
        }

        public Vector2D(double x, double y)
            : base(2)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Attribute to show param information
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    class ParamInfoAttribute : Attribute
    {
        public string HelpText { get; set; }
    }

    [AttributeUsage(AttributeTargets.All)]
    class ExcludeFromParamsAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All)]
    class ExcludeFromMD5Calculation : Attribute { }
}
