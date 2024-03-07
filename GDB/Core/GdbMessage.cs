using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core
{
    public class GdbMessage
    {
        public string Original { get; set; }

        public string Naked
        {
            get
            {
                if (string.IsNullOrEmpty(Original))
                {
                    return "";
                }
                if (Original[0] == '+')
                {
                    return Original.Length > 4 ? Original.Substring(2, Original.Length - 5) : Original;
                }
                else if (Original[0] == '$')
                {
                    return Original.Length > 3 ? Original.Substring(1, Original.Length - 4) : Original;
                }
                else
                {
                    return "";
                }
            }
        }

        public string Nakedxml
        {
            get
            {
                return string.IsNullOrEmpty(Naked) ? "" : Naked.Substring(1);
            }
        }

        public GdbMessage(string message)
        {
            Original = message;
        }

        public List<string> GetLines(bool nake = true)
        {
            var protolines = new List<string>();
            foreach (var item in Original.Split('$'))
            {
                if (item.Length > 3)
                    protolines.Add(item.Substring(0, item.Length - 3));
            }
            return protolines;
        }
    }
}
