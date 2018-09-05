using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Net.Mail;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Security.Cryptography;



namespace wapro_export

{
    class Program
    {
        public static string flog = AppDomain.CurrentDomain.BaseDirectory + "errors_log.txt";
        public static string enc_fname = AppDomain.CurrentDomain.BaseDirectory + "encrypted_arg.txt";
       // public static string rest_ffname = AppDomain.CurrentDomain.BaseDirectory + "rest.csv";

        static void Main(string[] args)
        {
            // check exists error_log
            if (!File.Exists(flog)) { FileStream fs = File.Create(flog); fs.Close(); }
            try
            {
                CfgRead.mess("Hello. Wait, please");

                if (args.Length == 1)
                {
                    
                    encrypt_arg_to_file(args[0]);
                    MessageBox.Show(args[0] + " is encrypted to " + enc_fname);
                }
                else
                {
                    // read data from .xml & proceed with startup arg
                    run_queries_from_cfg(CfgRead.start_arg01);

                    // write changed data data to .xml
                    foreach (write_params wp in CfgRead.db_list_write_params) CfgRead.xml_save_node(wp);
                    // send mail (if needs)
                    CfgRead.do_mail_block(); //   do_mail_block();
                }
                CfgRead.mess("--- Job's done  ---");
            }
            catch (Exception e)
            {
                // StreamReader sr = File.OpenText(flog);
                File.AppendAllText(flog, "\n\n" + DateTime.Now.ToString() + "\n" + e.ToString());
                MessageBox.Show(e.ToString(), "Exception ERROR");
            }
            // Console.WriteLine("\n\n--- Bye, any key ---");Console.ReadKey();//.ReadLine();           
        }




        /// <summary>
        /// encrypt string (password) for writing to conn_str_pass into app_cfg.xml
        /// </summary>
        /// <param name="str"></param>
        static void encrypt_arg_to_file(string str)
        {
            if (File.Exists(enc_fname)) File.Delete(enc_fname);
            else 
            { FileStream fs = File.Create(enc_fname); fs.Close(); }
            File.AppendAllText(enc_fname, "input string:\n"+ str + "\n\nencrypted string:\n" + CfgRead.Encrypt(str));
        }




        /// <summary>
        ///  Write data to SQL from List<string>
        /// </summary>
        /// <param name="flag"></param>
        static void run_queries_from_cfg(int flag)
        {

            // работа с базами
            foreach (dbproc dbinfo in CfgRead.db_list)
            {
                dbinfo.conn();
                List<List<string>> docs_num_list;
                List<List<string>> result_list;
                string last_s = "";

                //   List<string> heapCsv_tmp = new List<string>();

                //-------------------------------------------------------------------------------

                // ---------------------    1  Get Data From MS SQL Wapro  ----------------------

                //-------------------------------------------------------------------------------
                if (dbinfo.id == 1 && flag==1)
                {
                    // get list of new records into docs_num_list as params
                    docs_num_list = reader_to_list(dbinfo.label,dbinfo.connection, String.Format(dbinfo.qu[0], dbinfo.param[0]), false, true,false);
                   // foreach (List<string> s in docs_num_list) Console.WriteLine(s[0]);

                    foreach (List<string> s in docs_num_list)
                    {
                       
                       // Console.WriteLine("* * * * * * * * * " + s +  " * * * *");
                        result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[1], s[0]), true,true,true);
                        result_list.AddRange(reader_to_list(dbinfo.label,dbinfo.connection, String.Format(dbinfo.qu[2], s[0]),true, true,true));
                        result_list.AddRange(reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[3], s[0]), true, true, true));

                        // Add doc to out_data
                        out_data od = new out_data
                        { doc_heap_list = result_list
                         , doc_fname = result_list[1][2].Replace('/', '-').Trim() +"."+ CfgRead.file_export_type //         result_list[1].Split(';')[2].Replace('/','-')+".csv",
                         , doc_heap = CfgRead.split_double_list(result_list, CfgRead.file_export_type)
                         , id_plat = result_list[1][0]
                         , id_doc_handl = result_list[1][1]
                        };

                        CfgRead.out_data_list.Add(od);

                        last_s = s[0];
                    }
                    // write params to config .xml
                    if (last_s != "")
                    {
                        write_params wp = new write_params();
                        wp.db_conf_id = dbinfo.id;
                        wp.param_val = last_s;
                        CfgRead.db_list_write_params.Add(wp);
                    }
                }
                //-------------------------------------------------------------------------------

                // ------------------------    2  write data to MySQL   -------------------------

                //-------------------------------------------------------------------------------
                //  Console.WriteLine("# # # # # # # #  dbinfo.id = " + dbinfo.id.ToString() + " # # # # # # # # ");
                if (dbinfo.id == 2 && flag==1)
                {
                    //--------------- write to table wapro_doc ---------------------------------
                    foreach (out_data dd in CfgRead.out_data_list)
                    {
                        result_list = reader_to_list(dbinfo.label,
                            dbinfo.connection
                            , String.Format(dbinfo.qu[1], dd.doc_fname,dd.doc_heap.Replace("'","\""), dd.id_plat, dd.id_doc_handl)
                            , false,false,false);
                    }
                }
                //-------------------------------------------------------------------------------

                // --------------    3  get data from MySQL  and write to files -----------------

                //-------------------------------------------------------------------------------
                if (dbinfo.id == 3 && flag==2)
                {
                  //   Console.WriteLine("  dbinfo.id = " + dbinfo.id.ToString() );
                    last_s = "";

                    result_list = reader_to_list(dbinfo.label,dbinfo.connection, String.Format(dbinfo.qu[0], dbinfo.param[0]), false, true,false);

                    if (result_list.Count > 0)
                    {
                        foreach (List<string> ss in result_list) File.WriteAllText(CfgRead.file_export_dir + ss[1], ss[2]);

                        // get id of last record in result_list
                        last_s = result_list[result_list.Count - 1][0].ToString();

                        if (last_s != "")
                        {
                            write_params wp = new write_params();
                            wp.db_conf_id = dbinfo.id;
                            wp.param_val = last_s;
                            CfgRead.db_list_write_params.Add(wp);
                        }
                    }
                }
                //-------------------   R E S T   E X P O R T WAPRO->Magento    --------------------

                // ------------ 1.   id=4  get data from MySQL  and write to files -----------------

                //----------------------------------------------------------------------------------

                if (dbinfo.id == 4 && flag == 3)
                {
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[0]), true, true, false);

                    List<string> stmp = new List<string>();
                    foreach(List<string> s in result_list) stmp.Add( s[0]+";"+s[1]);
                    // rest_ffname
                    File.WriteAllLines(CfgRead.csv_source_ffn, stmp.ToArray());
                  
                }
                if (dbinfo.id == 5 && flag == 3)
                {
                    // this trick change \ to \\ in path
                  //  string rest_ffname_double_slash=System.Text.RegularExpressions.Regex.Replace(rest_ffname, @"\\", @"\\");
                   // rest_ffname_double_slash = CfgRead.csv_source_ffn;
                   //   CfgRead.mess(rest_ffname_double_slash);
                    // File.WriteAllText("d:\\aaa.txt", string.Format(dbinfo.qu[2], ss, dbinfo.param[0]));
                    //  &#38;
                   
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[0], dbinfo.param[0]), true,false, false);
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[1], dbinfo.param[0]), true, false, false);
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[2], CfgRead.csv_source_ffn, dbinfo.param[0]), true, false, false);
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, dbinfo.qu[3], false, false, false);
                    result_list = reader_to_list(dbinfo.label, dbinfo.connection, String.Format(dbinfo.qu[4], dbinfo.param[0]), false, false, false);
                }
                //------------------- E N D     R E S T   E X P O R T WAPRO->Magento    --------------------


            }
        }



        /// <summary>
        ///  Read data from SQL and write result to List<List<string>> 
        /// </summary>
        /// <param name="conn_label"></param>
        /// <param name="connection"></param>
        /// <param name="qu"></param>
        /// <param name="add_field_name"></param>
        /// <param name="is_select_query"></param>
        /// <param name="replace_semicolon"></param>
        /// <returns></returns>
        static List<List<string>> reader_to_list(string conn_label,IDbConnection connection, string qu,  Boolean add_field_name, Boolean is_select_query, Boolean replace_semicolon) //SqlConnection sqlConnection1,
        {
            List<List<string>> tmp02 = new List<List<string>>();
            List<string> tmp01 = new List<string>();
            IDbCommand command = connection.CreateCommand();
            command.CommandText = qu;

            try
            {
                connection.Open();
                
                Boolean f = true;
                
                if (is_select_query)
                {
                    using (IDataReader objReader = command.ExecuteReader())
                    {
                        if (!objReader.IsClosed) //.HasRows)
                        {
                            while (objReader.Read())
                            {
                                if (f && add_field_name)
                                {
                                    for (int i = 0; i < objReader.FieldCount; i++) tmp01.Add(objReader.GetName(i).ToString());// + sep;
                                    tmp02.Add(tmp01);
                                    tmp01 = new List<string>();
                                    f = false;
                                }
                                for (int i = 0; i < objReader.FieldCount; i++)

                                tmp01.Add(replace_semicolon ? objReader.GetValue(i).ToString().Replace(';', ',') : objReader.GetValue(i).ToString());// + sep;
                                tmp02.Add(tmp01);
                                tmp01 = new List<string>();
                            }
                        }
                    }
                }
                else command.ExecuteNonQuery();
              
                connection.Close();
               
            }
            catch (Exception ex)
            {
                CfgRead.mess(ex.ToString());
                CfgRead.error_flag = true;
                CfgRead.mail_message += "\n\n* * *  " + conn_label + "connection Exception error:\n " + ex.ToString() + "\n" + connection.ConnectionString +"\n\n"+command.CommandText+ "\n * * *\n";
            }
            return tmp02;
        }



        //static string check_replace_special_sym(string src, Boolean f)
        //{
        //    if (f)
        //    {
        //        src = src.Replace('\'', '"');
        //        return src.Replace(';', ',');
        //    }
        //    else 
        //}


        //----------------------------------------------------------------------------------------------

        //-------------------   write default config .xml  before use - backup exist .xml  ------------

        //----------------------------------------------------------------------------------------------
        /*
        static void xml_write_default_settins()
        {
            dbproc def = new dbproc
            {
                id = 1,
                type = "mssql",
                conn_str = "Server=192.168.1.36;Database=WAPRO;"
                +"User Id=sa; Password =Boxter1123  ; ",
                qu = new List<string>() {"qu1","qu2" },
                param = new List<string>() { "param1", "param2" }
            };

            XmlDocument doc = new XmlDocument();
            XmlNode docNode = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.AppendChild(docNode);

            XmlElement el = (XmlElement)doc.AppendChild(doc.CreateElement("db_conf"));
            //el.SetAttribute("Bar", "some & value");
            el.AppendChild(doc.CreateElement("id")).InnerText = def.id.ToString();
            el.AppendChild(doc.CreateElement("type")).InnerText = def.type;
            el.AppendChild(doc.CreateElement("conn_str")).InnerText = def.conn_str;

            append_child_list(el.AppendChild(doc.CreateElement("qu")),def.qu);
            append_child_list(el.AppendChild(doc.CreateElement("param")), def.param);

          // uncomment to write .xml;
          //  doc.Save("app_cfg.xml");

        }
        static void append_child_list(XmlNode el,List<string> ls)
        {
            for (int i = 0; i < ls.Count; i++)
                el.AppendChild(el.OwnerDocument.CreateElement(i.ToString())).InnerText = ls[i];

        }
        */            
    }
}
