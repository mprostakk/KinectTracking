using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace KinectHandTracking
{
    public class Variables
    {
        private static Variables variables;
        public static  Variables getInstance()
        {
            if(variables == null)
            {
                variables = new Variables();
            }
            return variables;
        }
        public int isHovering;
        public int thick = 10;
        public int getHovering()
        {
            return isHovering;
        }
    };
}