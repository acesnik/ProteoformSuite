using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;
using System.IO;
using Proteomics;
using Ionic.Zlib;

namespace ProteoformSuiteInternal
{
    public class SaveState
    {
        public static Lollipop default_settings = new Lollipop();

        //BASICS FOR XML WRITING
        public static XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
        {
            Indent = true,
            IndentChars = "  "
        };

        public static string time_stamp()
        {
            return DateTime.Now.Year.ToString("0000") + "-" + DateTime.Now.Month.ToString("00") + "-" + DateTime.Now.Day.ToString("00") + "-" + DateTime.Now.Hour.ToString("00") + "-" + DateTime.Now.Minute.ToString("00") + "-" + DateTime.Now.Second.ToString("00");
        }

        private static void initialize_doc(XmlWriter writer)
        {
            writer.WriteProcessingInstruction("xml", "version='1.0'");
            writer.WriteStartElement("proteoform_suite");
            writer.WriteAttributeString("documentVersion", "0.01");
            writer.WriteAttributeString("id", time_stamp());
        }

        private static void finalize_doc(XmlWriter writer)
        {
            writer.WriteEndElement();
            writer.WriteEndDocument();
        }

        private static string GetAttribute(XElement element, string attribute_name)
        {
            XAttribute attribute = element.Attributes().FirstOrDefault(a => a.Name.LocalName == attribute_name);
            return attribute == null ? "" : attribute.Value;
        }


        //METHOD SAVE/LOAD
        public static StringBuilder save_method(StringBuilder builder)
        {
            using (XmlWriter writer = XmlWriter.Create(builder, xmlWriterSettings))
            {
                initialize_doc(writer);
                add_settings(writer);
                finalize_doc(writer);
            }
            return builder;
        }
        public static StringBuilder save_method()
        {
            return save_method(new StringBuilder());
        }

        private static void add_settings(XmlWriter writer)
        {
            //Gather field type, name, values that are not constants, which are literal, i.e. set at compile time
            //Note that fields do not have {get;set;} methods, where Properties do.
            foreach (FieldInfo field in typeof(Lollipop).GetFields().Where(f => !f.IsLiteral))
            {
                if (field.FieldType == typeof(int) ||
                    field.FieldType == typeof(double) ||
                    field.FieldType == typeof(string) ||
                    field.FieldType == typeof(decimal) ||
                    field.FieldType == typeof(bool))
                {
                    writer.WriteStartElement("setting");
                    writer.WriteAttributeString("field_type", field.FieldType.FullName);
                    writer.WriteAttributeString("field_name", field.Name);
                    writer.WriteAttributeString("field_default", field.GetValue(default_settings).ToString());
                    writer.WriteAttributeString("field_value", field.GetValue(null).ToString());
                    writer.WriteEndElement();
                }
            }
        }

        public static void open_method(string text)
        {
            FieldInfo[] lollipop_fields = typeof(Lollipop).GetFields();
            List<XElement> settings = new List<XElement>();
            using (XmlReader reader = XmlReader.Create(new StringReader(text)))
            {
                reader.MoveToContent();
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        if (reader.Name == "setting") settings.Add(XElement.ReadFrom(reader) as XElement);
                        else return; //Settings come first. Return when done
                    }
                }
            }

            foreach (XElement setting in settings)
            {
                string type_string = GetAttribute(setting, "field_type");
                Type type = Type.GetType(type_string); //Takes only full name of type
                string name = GetAttribute(setting, "field_name");
                string value = GetAttribute(setting, "field_value");
                lollipop_fields.FirstOrDefault(p => p.Name == name).SetValue(null, Convert.ChangeType(value, type));
            }
        }
        public static void open_method(string[] lines)
        {
            open_method(String.Join(Environment.NewLine, lines));
        }


        //FULL SAVE STATE -- this is a start, but not implemented or tested, yet
        public static StringBuilder save_all(StringBuilder builder)
        {
            using (XmlWriter writer = XmlWriter.Create(builder, xmlWriterSettings))
            {
                initialize_doc(writer);
                add_settings(writer);
                add_objects(writer);
                finalize_doc(writer);
            }
            return builder;
        }

        public static void save_all(string file_path)
        {
            File.WriteAllText(file_path, save_all(new StringBuilder()).ToString());
        }

        private static void add_objects(XmlWriter writer)
        {
            List<FieldInfo> lollipop_objects = typeof(Lollipop).GetFields().Where(f => !f.IsLiteral && f.FieldType.IsClass).ToList();
            List<FieldInfo> lollipop_enumerables = typeof(Lollipop).GetFields().Where(f => !f.IsLiteral && f.FieldType.IsClass && f.FieldType != typeof(string) && f.GetValue(null) as IEnumerable != null).ToList();
            object community = lollipop_objects.FirstOrDefault(f => f.FieldType == typeof(ProteoformCommunity)).GetValue(null); //Get the instance of community to check for enumerables. If we start making more than one community, we'll have to do this in a foreach.
            IEnumerable<FieldInfo> community_enumerables = typeof(ProteoformCommunity).GetFields().Where(f => !f.IsLiteral && f.FieldType.IsClass && f.GetValue(community) as IEnumerable != null);

            //write_enumerable_group(typeof(KeyValuePair<string, List<Modification>>), lollipop_enumerables, writer);
            //write_enumerable_group(typeof(InputFile), lollipop_enumerables, writer);
            write_enumerable_group(typeof(Component), lollipop_enumerables, writer);
            //write_enumerable_group(typeof(NeuCodePair), lollipop_enumerables, writer);
            //write_enumerable_group(typeof(ExperimentalProteoform), community_enumerables, writer);
            //write_enumerable_group(typeof(TheoreticalProteoform), community_enumerables, writer);
            //write_enumerable_group(typeof(KeyValuePair<string, TheoreticalProteoform>), community_enumerables, writer);
            //write_enumerable_group(typeof(ProteoformRelation), lollipop_enumerables, writer);
            //write_enumerable_group(typeof(KeyValuePair<string, ProteoformRelation>), lollipop_enumerables, writer);
            //write_enumerable_group(typeof(DeltaMassPeak), community_enumerables, writer);
            //write_enumerable_group(typeof(ProteoformFamily), community_enumerables, writer);
        }

        //There might be multiple lists with objects of a certain type, so start write out each of those lists
        private static void write_enumerable_group(Type type, IEnumerable<FieldInfo> lollipop_enumerables, XmlWriter writer)
        {
            List<FieldInfo> enumerables = lollipop_enumerables.Where(e => e.FieldType.GetInterfaces().Where(i => i.IsGenericType).Select(t => t.GetGenericArguments()[0]).All(t => t == type)).ToList();
            foreach (FieldInfo field in enumerables) write_enumerable(field, field.GetValue(null), writer);
        }

        //Given a list of a certain type, write out the elements of that list
        private static void write_enumerable(FieldInfo field, object a, XmlWriter writer)
        {
            writer.WriteStartElement("enumerable");
            writer.WriteAttributeString("field_name", field.Name);
            writer.WriteAttributeString("field_type", field.FieldType.Name);
            foreach (object item in a as IEnumerable) write_object(field.FieldType.GetInterfaces().Where(i => i.IsGenericType).Select(t => t.GetGenericArguments()[0]).FirstOrDefault(), item, writer);
            writer.WriteEndElement();
        }

        private static void write_enumerable(PropertyInfo property, object a, XmlWriter writer)
        {
            writer.WriteStartElement("enumerable");
            writer.WriteAttributeString("property_name", property.Name);
            writer.WriteAttributeString("property_type", property.PropertyType.Name);
            foreach (object item in a as IEnumerable) write_object(property.PropertyType.GetInterfaces().Where(i => i.IsGenericType).Select(t => t.GetGenericArguments()[0]).FirstOrDefault(), item, writer);
            writer.WriteEndElement();
        }

        //For an object in a list, write out the object properties, maintaining the same exclusion list
        private static void write_object(Type type, object a, XmlWriter writer)
        {
            writer.WriteStartElement("object");
            writer.WriteAttributeString("object_type", type.Name);
            foreach (FieldInfo field in type.GetFields()) //get all of the fields of this object type
            {
                Type property_type = field.FieldType;
                string type_name = property_type.Name;
                string property_name = field.Name;
                if (property_type != typeof(string))
                    if (field.GetValue(a) as IEnumerable != null)
                    {
                        writer.WriteStartElement("enumerable");
                        writer.WriteAttributeString("field_name", property_name);
                        writer.WriteAttributeString("field_type", type_name);
                        if (field.FieldType == typeof(List<ChargeState>)) write_enumerable(field, field.GetValue(a), writer);
                        else writer.WriteAttributeString("property_value", field.GetValue(a).ToString());
                        writer.WriteEndElement();
                    }
                    else if (property_type.IsClass) //If not an enumerable, is it still a class?
                    {
                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("field_name", property_name);
                        writer.WriteAttributeString("field_type", type_name);
                        write_object(property_type, field.GetValue(a), writer);
                        writer.WriteEndElement();
                    }
                    else
                    {
                        writer.WriteStartElement("scalar");
                        writer.WriteAttributeString("field_name", property_name);
                        writer.WriteAttributeString("field_type", type_name);
                        writer.WriteAttributeString("field_value", field.GetValue(a).ToString());
                        writer.WriteEndElement();
                    }
            }
            foreach (PropertyInfo property in type.GetProperties()) //get all of the properties of this object type
            {
                Type property_type = property.PropertyType;
                string type_name = property_type.Name;
                string property_name = property.Name;
                if (property_type != typeof(string))
                    if (property.GetValue(a) as IEnumerable != null)
                {
                    writer.WriteStartElement("enumerable");
                    writer.WriteAttributeString("property_name", property_name);
                    writer.WriteAttributeString("property_type", type_name);
                    if (property.PropertyType == typeof(List<ChargeState>)) write_enumerable(property, property.GetValue(a), writer);
                    else writer.WriteAttributeString("property_value", property.GetValue(a).ToString());
                    writer.WriteEndElement();
                }
                else if (property_type.IsClass) //If not an enumerable, is it still a class?
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("property_name", property_name);
                    writer.WriteAttributeString("property_type", type_name);
                    write_object(property_type, property.GetValue(a), writer);
                    writer.WriteEndElement();
                }
                else
                {
                    writer.WriteStartElement("scalar");
                    writer.WriteAttributeString("property_name", property_name);
                    writer.WriteAttributeString("property_type", type_name);
                    writer.WriteAttributeString("property_value", property.GetValue(a).ToString());
                    writer.WriteEndElement();
                }
            }
            writer.WriteEndElement();
        }

        //OPEN SAVE STATE -- incomplete
        //Requires that each field with a complex setter method also have a field that can be directly set.
        public static void open_all(string file_path)
        {
            FieldInfo[] lollipop_fields = typeof(Lollipop).GetFields();
            List<XElement> settings = new List<XElement>();
            string ptmlist = "";
            List<XElement> enumerables = new List<XElement>();
            using (FileStream stream = new FileStream(file_path, FileMode.Open))
            {
                Stream save_stream = stream;
                if (file_path.EndsWith(".gz"))
                    save_stream = new GZipStream(stream, CompressionMode.Decompress);
                using (XmlReader reader = XmlReader.Create(save_stream))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            if (reader.Name == "setting") settings.Add(XElement.ReadFrom(reader) as XElement);
                            else if (reader.Name == "ptmlist") ptmlist = (XElement.ReadFrom(reader) as XElement).Value;
                            else if (reader.Name == "enumerable") enumerables.Add(XElement.ReadFrom(reader) as XElement);
                            else return; //that's it for now
                        }
                    }
                }
            }

            foreach (XElement setting in settings)
            {
                string type_string = GetAttribute(setting, "field_type");
                Type type = Type.GetType(type_string); //Takes only full name of type
                string name = GetAttribute(setting, "field_name");
                string value = GetAttribute(setting, "field_value");
                lollipop_fields.FirstOrDefault(p => p.Name == name).SetValue(null, Convert.ChangeType(value, type));
            }
        }
    }
}
