using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading.Tasks;

using System.Xml.Linq;
using System.Xml.XPath;
using MySql.Data.MySqlClient;
using System.Data;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;

namespace wapro_export
{
    public static class CfgRead
    {
        public static string cfg_file_name = AppDomain.CurrentDomain.BaseDirectory + "app_cfg.xml";
        public static string tname = "_tmp_rest_rest_01";
        public static string fprefix = "rest_";
        public static string mail_message = "";
        public static Boolean error_flag = false;
        public static Boolean send_mail_on_error = true;
        public static Boolean send_mail_on_success = false;
        public static Boolean is_pass_encrypted = false;
        public static string send_mail_on_error_to = "";
        public static string csv_source_ffn = "";
        public static string file_export_dir = "";
        public static string file_export_type = "";
        public static string fields_naming_lang = "";
        public static string ek = "";
        public static int start_arg01;
        public static  Boolean show_addon_messages = false;

        public static IEnumerable<dbproc> db_list;
        public static List<write_params> db_list_write_params =new List<write_params>();
        public static cfg_mail cfm;
        public static List<out_data> out_data_list = new List<out_data>();

        /// <summary>
        ///  show message is allowed
        /// </summary>
        /// <param name="s"></param>
        public static void mess(string s)
        {
            if (show_addon_messages)  MessageBox.Show(s); //Console.WriteLine(s); //
        }




        static CfgRead()
        {
                XDocument xd = XDocument.Load(cfg_file_name);
                is_pass_encrypted = Boolean.Parse(xd.Root.Element("is_pass_encrypted").Value.ToString());
                db_list = from c in xd.Descendants("db_conf")
                      select new dbproc()
                      {
                          label = (string)c.Element("label").Value,
                          type = (string)c.Element("type").Value,
                          
                          id = System.Convert.ToInt16(c.Element("id").Value),
                          qu = get_list(c.Descendants("qu").Elements()),
                          param = get_list(c.Descendants("param").Elements()),
                          conn_str = String.Format(
                              (string)c.Element("conn_str").Value,
                              (is_pass_encrypted ? 
                              Decrypt((string)c.Element("conn_str_pass").Value)
                              :(string)c.Element("conn_str_pass").Value)
                          )
                      };
                if (String.Compare(xd.Root.Element("send_mail_on_error").Value.ToString(), "false", true) == 0) send_mail_on_error = false;
                if (String.Compare(xd.Root.Element("send_mail_on_success").Value.ToString(), "true", true) == 0) send_mail_on_success = true;
                send_mail_on_error_to = xd.Root.Element("send_mail_on_error_to").Value.ToString();
                csv_source_ffn = xd.Root.Element("csv_source_ffn").Value.ToString();
                file_export_dir = xd.Root.Element("file_export_dir").Value.ToString();
                file_export_type = xd.Root.Element("file_export_type").Value.ToString();
                fields_naming_lang = xd.Root.Element("fields_naming_lang").Value.ToString();
                start_arg01 = Int32.Parse(xd.Root.Element("start_arg01").Value.ToString());
                show_addon_messages = Boolean.Parse(xd.Root.Element("show_addon_messages").Value.ToString());
                ek = xd.Root.Element("ek").Value.ToString();
                // fill mail data
                foreach (var ms in xd.Descendants(xd.Root.Element("use_mail_node").Value.ToString()))
                {
                    cfm = new cfg_mail()
                    {
                        pop = (string)ms.Element("pop").Value,
                        smtp = (string)ms.Element("smtp").Value,
                        pop_port = Convert.ToInt32(ms.Element("pop_port").Value.ToString()),
                        smtp_port = Convert.ToInt32(ms.Element("smtp_port").Value.ToString()),
                        user = (string)ms.Element("user").Value,
                        pass = is_pass_encrypted ? Decrypt((string)ms.Element("pass").Value) : (string)ms.Element("pass").Value,
                        enablessl = Convert.ToBoolean(ms.Element("enablessl").Value.ToString())
                    };
                }  
        }


        //  -----------------    ENCRYPT-DECRYPT BLOCK ------------------ //
        
        /// <summary>
        /// decryprion function 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <returns></returns>
        /// 
        public static string Decrypt(string cipherText)
        {
            string EncryptionKey = ek;
            cipherText = cipherText.Replace(" ", "+");
            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(cipherBytes, 0, cipherBytes.Length);
                        cs.Close();
                    }
                    cipherText = Encoding.Unicode.GetString(ms.ToArray());
                }
            }
           // MessageBox.Show(cipherText);
            return cipherText;
        }

        /// <summary>
        /// Encription function
        /// </summary>
        /// <param name="clearText"></param>
        /// <returns></returns>
        public static string Encrypt(string clearText)
        {
            string EncryptionKey =  ek;
            byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
            using (Aes encryptor = Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        cs.Write(clearBytes, 0, clearBytes.Length);
                        cs.Close();
                    }
                    clearText = Convert.ToBase64String(ms.ToArray());
                }
            }
            return clearText;
        }
        //  -----------------  END  ENCRYPT-DECRYPT BLOCK ------------------ //


        /// <summary>
        /// convert list<list <string>> to .csv or .xml string
        /// </summary>
        /// <param name="llist"></param>
        /// <param name="file_export_type"></param>
        /// <returns></returns>
        public static string split_double_list(List<List<string>> llist, string file_export_type)
        {
            string ret_str="";
            if (file_export_type == "csv")
            {
                List<string> stmp2 = new List<string>();
                foreach (List<string> stmp in llist) stmp2.Add(string.Join(";", stmp.ToArray()));
                ret_str = string.Join("\r\n", stmp2.ToArray());
            }
            else if (file_export_type == "xml")
            {
                ret_str = create_xml(llist);
            }
            return ret_str;
        }


        public static string create_xml( List<List<string>>llist)
        {
            XDocument xd = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));




            // XElement elem =  new XElement("Document");
            string Document_branche = CfgRead.fields_naming_lang == "ru" ? "Документ" : "Document";
            string Goods_branche = CfgRead.fields_naming_lang == "ru" ? "Товары" : "GOODS";
            string Goods_node = CfgRead.fields_naming_lang == "ru" ? "Товар" : "GOODS_ITEM";
            string Contragents_branche = CfgRead.fields_naming_lang == "ru" ? "Контрагенты" : "CONTRAGENTS";
            string Contragents_node = CfgRead.fields_naming_lang == "ru" ? "Контрагент" : "CONTRAGENT";

            XElement elem = new XElement(create_xml_branche(llist[0], llist.GetRange(1, 1), Document_branche, ""));
            

            //  XElement elem_contragents = new XElement(Contragents_branche);
            // elem_contragents.Add(create_xml_branche(llist[2], llist.GetRange(3, 1), Contragents_node, ""));
            // elem.Add(elem_contragents);


            elem.Add(create_xml_branche(llist[2], llist.GetRange(3, 1), Contragents_branche, Contragents_node));
            elem.Add(create_xml_branche(llist[4], llist.GetRange(5, llist.Count - 5), Goods_branche, Goods_node));


            xd.Add(elem);

            string decl="";
            if (CfgRead.fields_naming_lang == "ru")
            {
                decl = @"<КоммерческаяИнформация ВерсияСхемы=""2.03"" ДатаФормирования=""2008-03-28"">" + Environment.NewLine 
                    + xd.ToString()
                    + Environment.NewLine + @"</КоммерческаяИнформация>";

            }
            else decl = xd.Declaration.ToString() + Environment.NewLine + xd.ToString();

            return decl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="llist"></param>
        /// <returns></returns>
        public static XElement create_xml_branche(List<string> head_list, List<List<string>> llist, string branche,string branche_node)
        {

            
            XElement el  = new XElement(branche);
            XElement ell = new XElement("tmp");

            if (branche_node != "")
            {
               el.Name  = branche_node;
               ell.Name = branche;
            }

            foreach (List<string> stmp in llist)
            {
                if (CfgRead.fields_naming_lang == "ru" && branche == "Документ") el.Add(new XElement("ХозОперация", "Заказ товара"));
                    for (int i = 0; i < head_list.Count; i++)
                {
                    el.Add(new XElement(head_list[i], stmp[i]));
                }
                if (branche_node != "") { ell.Add(el); el = new XElement(branche_node); }
            }
            if (branche_node != "") return ell;
            else return el;
        }



        /// <summary>
        ///  save changes to .xml file
        /// </summary>
        /// <param name="wp"></param>
        public static void xml_save_node(write_params wp)
        {
            string xml_fname = cfg_file_name;
            int i = 0;
            XDocument xml = XDocument.Load(xml_fname);
                    var db_conf_node = from c in xml.Root.Descendants("db_conf")
                              where c.Element("id").Value.Contains(wp.db_conf_id.ToString())
                              select c;
                     var param_node = db_conf_node.Descendants("param");
                        foreach (var cc in param_node)
                        {
                            if(i==wp.param_num)cc.Element("s").Value = wp.param_val;
                            i++;
                        }
            xml.Save(xml_fname);
        }

        // add nodes to List<string>
        static List<string> get_list(IEnumerable<XElement> xx)
        {
            List<string> ss = new List<string>();
            foreach (XElement xxe in xx)ss.Add(xxe.Value);
            return ss;
        }

        public static void do_mail_block()
        {
            // -------------------------------- MAIL SEND BLOCK -------------------------------------------
            if ((send_mail_on_error && error_flag) || (send_mail_on_success && !error_flag))
            {
                // Console.WriteLine("\n\n sending mail.....");
                try
                {
                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress(cfm.user);
                    mail.To.Add(new MailAddress(send_mail_on_error_to));
                    mail.Subject = "-- WAPRO export errors";

                    mail.Body = mail_message;
                    SmtpClient client = new SmtpClient();
                    client.Host = cfm.smtp;
                    client.Port = cfm.smtp_port;
                    client.EnableSsl = cfm.enablessl;
                    client.Credentials = new NetworkCredential(cfm.user, cfm.pass);
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;

                    if ((send_mail_on_success && !error_flag))
                    {
                        mail.Subject = "--- WAPRO export success";
                        client.Send(mail);
                    }
                    if ((send_mail_on_error && error_flag))
                    {
                        client.Send(mail);
                    }
                    client.Timeout = 20000;
                    mail.Dispose();
                }
                catch (Exception e)
                {
                    throw new Exception("Mail.Send: " + e.Message);
                }
                //  Console.WriteLine("\n\n mail sent.");
            } // -------------------------- END MAIL SEND BLOCK -------------------------------------
        }
    };

    public class write_params
    {
        public int db_conf_id;
        public int param_num;
        public string param_val;
    };

    public class dbproc
    {
        public string type;
       // public string file_export_type;
        public string label;
        public int id;
        public string conn_str;
        public List<string> qu; 
        public List<string> param;
        public IDbConnection connection;

        public void conn() // make abstract
        {
                if (this.type == "mssql") this.connection = new SqlConnection(this.conn_str);
                if (this.type == "mysql") this.connection = new MySqlConnection(this.conn_str);
        }
    };

    public class cfg_mail
    {
        public string pop { get; set; }
        public string smtp { get; set; }
        public int pop_port { get; set; }
        public int smtp_port { get; set; }
        public string user { get; set; }
        public string pass { get; set; }
        public Boolean enablessl { get; set; }
    };

    public class out_data
    {
        public List<List<string>> doc_heap_list;
        public string doc_fname;
        public string doc_heap;
        public string id_plat;
        public string id_doc_handl;
    };

    //public class db_proc_list
    //{
    //    IEnumerable<db_proc> dc;
    //    List<string> data_head;
    //    List<string> data_lst;
    //}
}