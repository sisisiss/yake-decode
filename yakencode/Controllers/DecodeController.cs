using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text.RegularExpressions;
using System.Collections;

namespace yakencode.Controllers
{
    public class DecodeController : Controller
    {
        // GET: Decode
        public ActionResult Index()
        {
            return View();
        }
        public string Decode()
        {
            string strsrc = HttpUtility.UrlDecode(Request.Form["srctext"]);
            bool bFormat = Boolean.Parse(Request.Form["format"].ToString());
            bool bTagKeep = Boolean.Parse(Request.Form["tagkeep"].ToString());
            bool beveryk = Boolean.Parse(Request.Form["everyk"].ToString());
            bool btagsmultiline = Boolean.Parse(Request.Form["tagsmultiline"].ToString());
            bool bstringdecode = Boolean.Parse(Request.Form["stringdecode"].ToString());
            string strresult = "";
            bool bprint = false;
            string tagpat = @"\s*[\w_\d]{5,6}";
            string pattwotag = @"^\s*([\w_\d]{5,6}:\s*){2,}(.*)";
            string patgoto = @"[\s]*goto[\s]+(?<gototag>[\w]+);";

            strsrc = new Regex(@"(;)").Replace(strsrc, "$1\r\n");//;前加换行
            strsrc = new Regex(@"(?<goto1>[}])(?<goto2>[\s]+goto\s\w*;)").Replace(strsrc, "${goto1}\r\n${goto2}");//}goto ***goto前加换行
            strsrc = new Regex(@"(?<goto1>[{])(?<goto2>[\s]+goto\s\w*;)").Replace(strsrc, "${goto1}\r\n${goto2}");//{goto ***{前加换行

            MatchEvaluator evaluator = new MatchEvaluator(WordScrambler);
            Stack stkscope = new Stack();


            try
            {
                string pat1 = "(\"([\\s\\S]+)?\")";
                if (bstringdecode)
                {
                    strsrc = Regex.Replace(strsrc, pat1, evaluator, RegexOptions.IgnorePatternWhitespace, Regex.InfiniteMatchTimeout);

                }


                strsrc = new Regex(@"(?<goto1>[{}])(?<goto2>[\s]*public\s+)").Replace(strsrc, "${goto1}\r\n${goto2}");//public前加换行
                strsrc = new Regex(@"(?<goto1>[{}])(?<goto2>[\s]*function\s+)").Replace(strsrc, "${goto1}\r\n${goto2}");//function前加换行
                strsrc = new Regex(@"(?<goto1>\s*[\w_\d]{5,6}:)\s*(?<goto2>}+)").Replace(strsrc, "${goto1}\r\n${goto2}");//}前加换行



                strsrc = new Regex(@"([\n]{2,})").Replace(strsrc, "\n");//替换\n+
                strsrc = new Regex(@"(\r\n+)").Replace(strsrc, "\n");//所有回车换行替换为/n

                ArrayList arraysrc = new ArrayList();
                arraysrc.AddRange(strsrc.Split(new char[] { '\n' }));

                //两个或以上跳转标签分行
                bool bsplittag = false;
                if(btagsmultiline)
                {
                    for (int k = 0; k < arraysrc.Count; k++)
                    {
                        string line = arraysrc[k].ToString();


                        Regex r = new Regex(pattwotag, RegexOptions.None);
                        Match m = r.Match(line);

                        if (m.Success)
                        {

                            Group g = m.Groups[1];
                            CaptureCollection cc = g.Captures;

                            string strmatch = arraysrc[k].ToString();
                            arraysrc.RemoveAt(k);

                            for (int t = 0; t < cc.Count; t++)
                            {
                                Capture c = cc[t];
                                arraysrc.Insert(k, c.Value + m.Groups[2].Captures[0]);

                            }

                        }

                    }
                }
                



                if (bFormat) return strsrc;
                if (btagsmultiline)
                {
                    for (int q = 0; q < arraysrc.Count; q++)
                    {
                        strresult += arraysrc[q].ToString() + "\r\n"; ;

                    }
                    return strresult;
                }

                // 查找出现的goto语句并找到goto后面的标志如e28xs;
                bool isInContralStatment = false;
                int nDepthContralStatment = 0;
                for (int k = 0; k < arraysrc.Count; k++)
                {
                    System.Diagnostics.Debug.Write("\r\nk*********" + k + "/" + arraysrc.Count + "******\r\n");
                    string line = arraysrc[k].ToString();

                    Regex r = new Regex(@"^\s*([\w_\d]{5,6}:\s*){0,}(.*)" + patgoto, RegexOptions.IgnoreCase);
                    Match m = r.Match(line);

                    if (m.Success)//处理当前goto的标签
                    {

                        Group g = m.Groups[3];
                        CaptureCollection cc = g.Captures;
                        Capture c = cc[0];
                        //查找标志对应的行并把查找到的行插入到goto后面
                        for (int l = k + 1; l < arraysrc.Count; l++)
                        {
                            bool bfind = false;
                            System.Diagnostics.Debug.Write("[l=" + l);

                            try
                            {
                                string strmatch = arraysrc[l].ToString();
                                //找到标签
                                if (new Regex(@"\s*(?<!goto\s*)" + c.Value, RegexOptions.None).Match(arraysrc[l].ToString()).Success)
                                {
                                    string patincludegoto = @".*\s+(?<=goto\s+)(?<goto1>" + tagpat + ");";
                                    //
                                    if (new Regex(pattwotag, RegexOptions.None).Match(strmatch.ToString()).Success)
                                    {
                                       
                                        break;
                                    }
                                    //判断在标签中包含goto语句，如有则在后面多加一条goto
                                    else if (new Regex(@".*\s+(?<=goto\s+)(?<goto1>" + tagpat + ");", RegexOptions.None).Match(strmatch.ToString()).Success)
                                    {

                                        if (l > k)
                                        {

                                            arraysrc.RemoveAt(l);
                                            arraysrc.Insert(k + 1, strmatch);
                                            strmatch = new Regex(patincludegoto, RegexOptions.None).Replace(strmatch, "goto ${goto1};");
                                            arraysrc.Insert(k + 2, strmatch);
                                            k++;
                                        }
                                        else
                                        {
                                            //arraysrc.Insert(k + 1, strmatch);
                                            //strmatch = new Regex(patincludegoto, RegexOptions.None).Replace(strmatch, "goto ${goto1};");
                                            //arraysrc.Insert(k + 2, strmatch);
                                        }
                                        break;
                                    }
                                    else if (new Regex(@"\s*if\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        isInContralStatment = true;
                                        nDepthContralStatment++;

                                        if (l > k)
                                        {

                                            arraysrc.RemoveAt(l);
                                            arraysrc.Insert(k + 1, strmatch);
                                            stkscope.Push(k + 2);
                                            int f = 1;

                                            do
                                            {
                                                strmatch = arraysrc[l + f].ToString();
                                                arraysrc.RemoveAt(l + f);
                                                arraysrc.Insert(k + 1 + f, strmatch);

                                                f++;
                                            } while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));

                                            strmatch = arraysrc[l + f].ToString();
                                            arraysrc.RemoveAt(l + f);
                                            arraysrc.Insert(k + f + 1, strmatch);

                                            f++;
                                            strmatch = arraysrc[l + f].ToString();
                                            arraysrc.RemoveAt(l + f);
                                            arraysrc.Insert(k + f + 1, strmatch);
                                            k++;
                                        }
                                        else
                                        {
                                        //    arraysrc.Insert(k + 1, strmatch);
                                        //    stkscope.Push(k + 2);
                                        //    int f = 1;

                                        //    do
                                        //    {
                                        //        strmatch = arraysrc[l + f].ToString();
                                        //        arraysrc.Insert(k + 1 + f, strmatch);

                                        //        f++;
                                        //    } while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));

                                        //    strmatch = arraysrc[l + f].ToString();
                                        //    arraysrc.Insert(k + f + 1, strmatch);

                                        //    f++;
                                        //    strmatch = arraysrc[l + f].ToString();
                                        //    arraysrc.Insert(k + f + 1, strmatch);
                                        //    k++;
                                        }

                                        // k = (int)stkscope.Pop() - 1;
                                        break;
                                    }
                                    else if (new Regex(@"\s*foreach\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        nDepthContralStatment++;
                                        if (nDepthContralStatment == 0)
                                        {
                                            if (l > k)
                                            {
                                                arraysrc.RemoveAt(l);
                                                arraysrc.Insert(k + 1, strmatch);
                                                stkscope.Push(k + 2);
                                                int f = 1;

                                                do
                                                {
                                                    strmatch = arraysrc[l + f].ToString();
                                                    arraysrc.RemoveAt(l + f);
                                                    arraysrc.Insert(k + 1 + f, strmatch);

                                                    f++;
                                                } while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));

                                                strmatch = arraysrc[l + f].ToString();
                                                arraysrc.RemoveAt(l + f);
                                                arraysrc.Insert(k + f + 1, strmatch);

                                                f++;
                                                strmatch = arraysrc[l + f].ToString();
                                                arraysrc.RemoveAt(l + f);
                                                arraysrc.Insert(k + f + 1, strmatch);
                                                k++;
                                                // k = (int)stkscope.Pop() - 1;
                                            }
                                            else
                                            {

                                                //arraysrc.Insert(k + 1, strmatch);
                                                //stkscope.Push(k + 2);
                                                //int f = 1;

                                                //do
                                                //{
                                                //    strmatch = arraysrc[l + f].ToString();

                                                //    arraysrc.Insert(k + 1 + f, strmatch);

                                                //    f++;
                                                //} while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));

                                                //strmatch = arraysrc[l + f].ToString();

                                                //arraysrc.Insert(k + f + 1, strmatch);

                                                //f++;
                                                //strmatch = arraysrc[l + f].ToString();

                                                //arraysrc.Insert(k + f + 1, strmatch);
                                                //k++;

                                            }
                                        }
                                        

                                        break;
                                    }
                                    else if (new Regex(@"\s*for\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        nDepthContralStatment++;
                                        if (nDepthContralStatment == 0)
                                        {
                                            if (l > k)
                                            {
                                                arraysrc.RemoveAt(l);
                                                arraysrc.Insert(k + 1, strmatch);
                                                stkscope.Push(k + 2);
                                                int f = 1;
                                                do
                                                {
                                                    strmatch = arraysrc[l + f].ToString();
                                                    arraysrc.RemoveAt(l + f);
                                                    arraysrc.Insert(k + 1 + f, strmatch);

                                                    f++;
                                                } while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));
                                                k = k + f;
                                                strmatch = arraysrc[l + f].ToString();
                                                arraysrc.RemoveAt(l + f);
                                                arraysrc.Insert(k + f + 1, strmatch);

                                                f++;
                                                strmatch = arraysrc[l + f].ToString();
                                                arraysrc.RemoveAt(l + f);
                                                arraysrc.Insert(k + f + 1, strmatch);
                                                k = k + f;
                                            }
                                            else
                                            {

                                                //arraysrc.Insert(k + 1, strmatch);
                                                //stkscope.Push(k + 2);
                                                //int f = 1;
                                                //do
                                                //{
                                                //    strmatch = arraysrc[l + f].ToString();
                                                //    arraysrc.Insert(k + 1 + f, strmatch);

                                                //    f++;
                                                //} while (!(new Regex(@"\s*}\s*", RegexOptions.None).Match(arraysrc[l + f].ToString()).Success));
                                                //k = k + f;
                                                //strmatch = arraysrc[l + f].ToString();
                                                //arraysrc.Insert(k + f + 1, strmatch);

                                                //f++;
                                                //strmatch = arraysrc[l + f].ToString();
                                                //arraysrc.Insert(k + f + 1, strmatch);
                                                //k = k + f;
                                            }
                                        }
                                        break;
                                    }
                                    else if (new Regex(@"\s*return\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        if (l > k)
                                        {
                                            arraysrc.RemoveAt(l);
                                            arraysrc.Insert(k + 1, strmatch);
                                            k++;
                                        }
                                        else
                                        {

                                            //arraysrc.Insert(k + 1, strmatch);
                                            //k++;
                                        }


                                        break;
                                    }
                                    else if (new Regex(@"\s*die\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        if (l > k)
                                        {
                                            arraysrc.RemoveAt(l);
                                            arraysrc.Insert(k + 1, strmatch);
                                            k++;
                                        }
                                        else
                                        {
                                            //arraysrc.Insert(k + 1, strmatch);
                                            //k++;
                                        }


                                        break;
                                    }
                                    else if (new Regex(tagpat + @":\s*$", RegexOptions.None).Match(strmatch).Success)
                                    {


                                        break;

                                    }
                                    else if (new Regex(@"\s}\s*", RegexOptions.None).Match(strmatch).Success)
                                    {
                                        if (--nDepthContralStatment < 0)
                                        {
                                            nDepthContralStatment = 0;
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        if (l > k)
                                        {
                                            arraysrc.RemoveAt(l);
                                            arraysrc.Insert(k + 1, strmatch);
                                            strmatch = arraysrc[l + 1].ToString();
                                            arraysrc.RemoveAt(l + 1);
                                            arraysrc.Insert(k + 2, strmatch);
                                            k++;
                                            break;
                                        }
                                        else
                                        {
                                            //arraysrc.Insert(k + 1, strmatch);
                                            //strmatch = arraysrc[l + 1].ToString();
                                            //arraysrc.Insert(k + 2, strmatch);
                                            //k++;
                                            break;

                                        }
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.Write("\r\n" + ex.Message + "!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + k + "!!!!!!!!!!!!!!!!!!!!\r\n");
                                break;

                            }
                            if (l == arraysrc.Count - 1)
                            {


                                l = 0;
                            }
                            if (k == l)
                            {
                                System.Diagnostics.Debug.Write("\r\n未找到标签" + c.Value + "++++++++++++++++++++++++++" + k + "++++++++++++++++++++++++\r\n");
                                break;
                            }

                        }

                    }

                    if (beveryk)
                    {

                        for (int q = 0; q < arraysrc.Count; q++)
                        {
                            strresult += arraysrc[q].ToString() + "\r\n"; ;
                        }

                    }

                    if (bprint)
                    {

                        for (int q = 0; q < arraysrc.Count; q++)
                        {
                            strresult += arraysrc[q].ToString() + "\r\n"; ;
                        }
                        return strresult;

                    }

                }

                //组合结果
                for (int q = 0; q < arraysrc.Count; q++)
                {
                    if (!bTagKeep)
                    {
                        arraysrc[q] = new Regex(@"\s*goto\s+[\w_\d]{5,6};").Replace(arraysrc[q].ToString(), "");
                        arraysrc[q] = new Regex(@"^\s*[\w_\d]{5,6}\s*:\s*").Replace(arraysrc[q].ToString(), "");
                        if (arraysrc[q].ToString() == "")
                        {
                            continue;
                        }
                    }

                    strresult += arraysrc[q].ToString() + "\r\n"; ;
                }

                return strresult;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex.Message);

                return strresult;
            }


        }
        public static string ProcessSlashString(Match match)
        {
            string retstr = "";
            

            string[] charcode = match.Captures[0].ToString().Trim(new char[] { '\"' }).Split(new char[] { '\\' });
            System.Text.Encoding utf8 = System.Text.Encoding.GetEncoding("utf-8");

            byte[] utf = new byte[charcode.Length + 1];
            utf[0] = (byte)Convert.ToChar('\"');
            try
            {
                int i = 1;
                foreach (string str in charcode)
                {

                    if (str == "")
                    {
                        continue;
                    }
                    string ichar = str;

                    char intchar;

                    if (!(ichar.Contains("X") || ichar.Contains("x")))
                    {

                        intchar = (Char)Convert.ToInt32(ichar, 8);

                    }
                    else
                    {
                        ichar = ichar.Trim(new[] { 'x', 'X' });
                        intchar = (Char)Convert.ToInt32(ichar, 16);
                    }

                    utf[i] = (byte)intchar;
                    i++;

                }



            }
            catch (Exception ex)
            {
                retstr = ex.Message;
            }

            utf[charcode.Length] = (byte)Convert.ToChar('\"');
            string str1 = utf8.GetString(utf);
            str1 = str1.Substring(1, str1.Length - 2);
            str1.Replace("\"", "\\\"");
           
            return str1;
        }
        public static string WordScrambler(Match match)
        {
            string retstr = "";
            string line = match.Captures[0].ToString();
            string pat2="(\\\\[xX][0-9a-fA-F]{1,2}|\\\\[0-9]{1,3})+";

            MatchEvaluator evaluator = new MatchEvaluator(WordScrambler);
           
            line = Regex.Replace(line, pat2, ProcessSlashString, RegexOptions.IgnorePatternWhitespace, Regex.InfiniteMatchTimeout);


            return line;

        }
    }
}