using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NLua;
using System.Windows.Forms;
using System.IO;

namespace calc_from_geometryOfMotor
{
    class LuaHelper
    {
        private static Lua lua_state;

        public static Lua GetLuaState()
        {
            if (lua_state != null)
                return lua_state;

            lua_state = new Lua();
            using(StreamReader sr = new StreamReader("lua_init.lua")) {
                String script = sr.ReadToEnd();
                lua_state.DoString(script);
            }

            return lua_state;
        }
    }
}
