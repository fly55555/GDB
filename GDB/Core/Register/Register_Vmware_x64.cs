using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GDB.Core.Register
{
    public class Register_Vmware_x64
    {
        public static string[] NameArray ={
            "RAX", "RBX", "RCX", "RDX",
            "RSI", "RDI", "RBP", "RSP",
            "R8 ", "R9 ", "R10", "R11",
            "R12", "R13", "R14", "R15",
            "RIP",
            "EFL",
            "CS ", "SS ", "DS ", "ES ", "FS ", "GS "};


        public static long[] GetArray(Context context)
        {
            var list = new List<long>();
            list.Add((long)context.rax);
            list.Add((long)context.rbx);
            list.Add((long)context.rcx);
            list.Add((long)context.rdx);

            list.Add((long)context.rsi);
            list.Add((long)context.rdi);
            list.Add((long)context.rbp);
            list.Add((long)context.rsp);

            list.Add((long)context.r8);
            list.Add((long)context.r9);
            list.Add((long)context.r10);
            list.Add((long)context.r11);

            list.Add((long)context.r12);
            list.Add((long)context.r13);
            list.Add((long)context.r14);
            list.Add((long)context.r15);

            list.Add((long)context.rip);
            list.Add((long)context.rflags);

            list.Add((long)context.cs);
            list.Add((long)context.ss);
            list.Add((long)context.ds);
            list.Add((long)context.es);
            list.Add((long)context.fs);
            list.Add((long)context.gs);

            return list.ToArray();
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable()]
        public struct Context
        {
            public ulong rax { get; set; }
            public ulong rbx { get; set; }
            public ulong rcx { get; set; }
            public ulong rdx { get; set; }
            public ulong rsi { get; set; }
            public ulong rdi { get; set; }
            public ulong rbp { get; set; }
            public ulong rsp { get; set; }
            public ulong r8 { get; set; }
            public ulong r9 { get; set; }
            public ulong r10 { get; set; }
            public ulong r11 { get; set; }
            public ulong r12 { get; set; }
            public ulong r13 { get; set; }
            public ulong r14 { get; set; }
            public ulong r15 { get; set; }
            public ulong rip { get; set; }
            public uint rflags { get; set; }
            public uint cs { get; set; }
            public uint ss { get; set; }
            public uint ds { get; set; }
            public uint es { get; set; }
            public uint fs { get; set; }
            public uint gs { get; set; }
        }




        //DEFAULT

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
        public partial class target
        {

            private string architectureField;

            private targetFeature featureField;

            private targetInclude[] includeField;

            private decimal versionField;

            /// <remarks/>
            public string architecture
            {
                get
                {
                    return this.architectureField;
                }
                set
                {
                    this.architectureField = value;
                }
            }

            /// <remarks/>
            public targetFeature feature
            {
                get
                {
                    return this.featureField;
                }
                set
                {
                    this.featureField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("include")]
            public targetInclude[] include
            {
                get
                {
                    return this.includeField;
                }
                set
                {
                    this.includeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public decimal version
            {
                get
                {
                    return this.versionField;
                }
                set
                {
                    this.versionField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class targetFeature
        {

            private targetFeatureFlags flagsField;

            private targetFeatureReg[] regField;

            private string nameField;

            /// <remarks/>
            public targetFeatureFlags flags
            {
                get
                {
                    return this.flagsField;
                }
                set
                {
                    this.flagsField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("reg")]
            public targetFeatureReg[] reg
            {
                get
                {
                    return this.regField;
                }
                set
                {
                    this.regField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class targetFeatureFlags
        {

            private targetFeatureFlagsField[] fieldField;

            private string idField;

            private byte sizeField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("field")]
            public targetFeatureFlagsField[] field
            {
                get
                {
                    return this.fieldField;
                }
                set
                {
                    this.fieldField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte size
            {
                get
                {
                    return this.sizeField;
                }
                set
                {
                    this.sizeField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class targetFeatureFlagsField
        {

            private string nameField;

            private byte startField;

            private byte endField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte start
            {
                get
                {
                    return this.startField;
                }
                set
                {
                    this.startField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte end
            {
                get
                {
                    return this.endField;
                }
                set
                {
                    this.endField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class targetFeatureReg
        {

            private string nameField;

            private byte bitsizeField;

            private byte regnumField;

            private string typeField;

            private string groupField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte bitsize
            {
                get
                {
                    return this.bitsizeField;
                }
                set
                {
                    this.bitsizeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte regnum
            {
                get
                {
                    return this.regnumField;
                }
                set
                {
                    this.regnumField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string group
            {
                get
                {
                    return this.groupField;
                }
                set
                {
                    this.groupField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class targetInclude
        {

            private string hrefField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string href
            {
                get
                {
                    return this.hrefField;
                }
                set
                {
                    this.hrefField = value;
                }
            }
        }







        //SSE

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "feature")]
        public partial class sse
        {

            private featureVector[] vectorField;

            private featureUnion unionField;

            private featureFlags flagsField;

            private featureReg[] regField;

            private string nameField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("vector")]
            public featureVector[] vector
            {
                get
                {
                    return this.vectorField;
                }
                set
                {
                    this.vectorField = value;
                }
            }

            /// <remarks/>
            public featureUnion union
            {
                get
                {
                    return this.unionField;
                }
                set
                {
                    this.unionField = value;
                }
            }

            /// <remarks/>
            public featureFlags flags
            {
                get
                {
                    return this.flagsField;
                }
                set
                {
                    this.flagsField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("reg")]
            public featureReg[] reg
            {
                get
                {
                    return this.regField;
                }
                set
                {
                    this.regField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureVector
        {

            private string idField;

            private string typeField;

            private byte countField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte count
            {
                get
                {
                    return this.countField;
                }
                set
                {
                    this.countField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureUnion
        {

            private featureUnionField[] fieldField;

            private string idField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("field")]
            public featureUnionField[] field
            {
                get
                {
                    return this.fieldField;
                }
                set
                {
                    this.fieldField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureUnionField
        {

            private string nameField;

            private string typeField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureFlags
        {

            private featureFlagsField[] fieldField;

            private string idField;

            private byte sizeField;

            /// <remarks/>
            [System.Xml.Serialization.XmlElementAttribute("field")]
            public featureFlagsField[] field
            {
                get
                {
                    return this.fieldField;
                }
                set
                {
                    this.fieldField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string id
            {
                get
                {
                    return this.idField;
                }
                set
                {
                    this.idField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte size
            {
                get
                {
                    return this.sizeField;
                }
                set
                {
                    this.sizeField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureFlagsField
        {

            private string nameField;

            private byte startField;

            private byte endField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte start
            {
                get
                {
                    return this.startField;
                }
                set
                {
                    this.startField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte end
            {
                get
                {
                    return this.endField;
                }
                set
                {
                    this.endField = value;
                }
            }
        }

        /// <remarks/>
        [System.SerializableAttribute()]
        [System.ComponentModel.DesignerCategoryAttribute("code")]
        [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
        public partial class featureReg
        {

            private string nameField;

            private byte bitsizeField;

            private byte regnumField;

            private string typeField;

            private string encodingField;

            private string formatField;

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string name
            {
                get
                {
                    return this.nameField;
                }
                set
                {
                    this.nameField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte bitsize
            {
                get
                {
                    return this.bitsizeField;
                }
                set
                {
                    this.bitsizeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public byte regnum
            {
                get
                {
                    return this.regnumField;
                }
                set
                {
                    this.regnumField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string type
            {
                get
                {
                    return this.typeField;
                }
                set
                {
                    this.typeField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string encoding
            {
                get
                {
                    return this.encodingField;
                }
                set
                {
                    this.encodingField = value;
                }
            }

            /// <remarks/>
            [System.Xml.Serialization.XmlAttributeAttribute()]
            public string format
            {
                get
                {
                    return this.formatField;
                }
                set
                {
                    this.formatField = value;
                }
            }
        }




    }

}
