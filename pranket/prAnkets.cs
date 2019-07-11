using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using Newtonsoft.Json;

namespace prAnket
{
    public partial class prAnkets : ServiceBase
    {
        Timer serviceTimer = new Timer();

        SqlCommand sqlCmd = new SqlCommand();
        SqlDataReader sqlDataReader;
        StreamWriter writer;

        string GSM, SabitTel, sonTelefon, ProtokolId, AdSoyad, GelisTipi = "A";
        private Database db;

        public prAnkets() { InitializeComponent(); }

        protected override void OnStart(string[] args)
        {
            try
            {
                dosyaIslem("    Program Çalıştırıldı.");
                fu();
                serviceTimer.Elapsed += new ElapsedEventHandler(Sorgula);
                serviceTimer.Interval = (3600000); //her de islem yap 1000*60sn*60dk
                serviceTimer.AutoReset = true;
                serviceTimer.Enabled = true;

                serviceTimer.Start();
            }
            catch (Exception ex) { dosyaIslem(ex.ToString()); }
        }
        protected override void OnStop() {
            dosyaIslem("    Program Durduruldu.");
        }

        private void fu()
        {
            try
            {
                if (Convert.ToInt64(DateTime.Now.Hour.ToString()) >= 9 && Convert.ToInt64(DateTime.Now.Hour.ToString()) <= 19)// canlida <=09 ve >=19 olacak
                {

                    db = new Database();

                    sqlDataReader = db.getHastaTelefonById();

                    while (sqlDataReader.Read())
                    {
                        GSM = sqlDataReader["GSM"].ToString().Trim();
                        SabitTel = sqlDataReader["SabitTel"].ToString().Trim();
                        ProtokolId = sqlDataReader["Id"].ToString().Trim();
                        AdSoyad = sqlDataReader["AdSoyad"].ToString().Trim();
                        GelisTipi = sqlDataReader["GelisTipiId"].ToString().Trim();

                        string gelenTel = Temizle(GSM);

                        if (gelenTel != "0") smsGonder(gelenTel, AdSoyad);
                    }
                    

                }
            }
            catch (Exception ex) { dosyaIslem(ex.ToString()); }
        }
        private void Sorgula(object sender, ElapsedEventArgs e)
        {
            try { fu(); }
            catch (Exception ex) { dosyaIslem(ex.ToString()); }
        }
        public string Temizle(string gsm)
        {
            if (TelefonOnay(gsm)) return sonTelefon;
            else if (TelefonOnay(SabitTel)) return sonTelefon;
            else return "0";
        }
        public bool TelefonOnay(string gsm)
        {
            try
            {
                if (!string.IsNullOrEmpty(gsm)) //telefon bos kayit edilmis olabilir.
                {

                    char[] separators = new char[] { '(', ')', '/', '-', '.', ' ' };
                    foreach (var c in separators) gsm = gsm.Replace(c.ToString(), string.Empty);

                    string[] temp;
                    temp = gsm.Split(separators, StringSplitOptions.RemoveEmptyEntries);
                    sonTelefon = String.Join("", temp);

                    string ilkHarf = sonTelefon.Substring(0, 1);//ilk harf ( olabiliyor o yuzden once fazlaliklari sildik.
                    if (ilkHarf == "0") sonTelefon = sonTelefon.Substring(1, sonTelefon.Length - 1);

                    string desen = @"^(5(\d{9}))$";
                    Match match = Regex.Match(sonTelefon, desen, RegexOptions.IgnoreCase);

                    return match.Success;
                }
                else return false;
            }
            catch (Exception ex) { dosyaIslem(ex.ToString()); throw; }
        }
        public void smsGonder(string numara, string adSoyad)
        {
            try
            {
                string mesaj = db.degiskenler.SMSIcerik;
                mesaj = Regex.Replace(mesaj, "xxx", adSoyad, RegexOptions.IgnoreCase);
                string sonId = postData();
                string url = "http://anket.5adim.com/?p=" + GelisTipi + ProtokolId + "." + sonId;
                mesaj += " " + url;

                using (WebClient client = new WebClient())
                {
                    string postUrl = "http://anket.5adim.com/servis/SMS.php/setSMS";

                    byte[] gelenYanit = client.UploadValues(postUrl, new NameValueCollection()
                     {
                        { "token",  "e37fd7e3df0fc392e925db0d7a901702" },
                        { "SMSKullanici",  db.degiskenler.SMSKullanici },
                        { "SMSKodu", db.degiskenler.SMSKodu },
                        { "SMSSifre",  db.degiskenler.SMSSifre },
                        { "SMSBaslik",  db.degiskenler.SMSBaslik },
                        { "mesaj",  mesaj },
                        { "telefon",  numara},
                        { "SMSSirket",  db.degiskenler.SMSSirket }
                     });

                    string result = Encoding.UTF8.GetString(gelenYanit);
                    DonenDegerler obj = JsonConvert.DeserializeObject<DonenDegerler>(result);

                    if (obj.sonuc != "success") dosyaIslem("Mesaj Gitmedi:" + mesaj);
                    else {  }
                }
            }
            catch (Exception ex) { dosyaIslem(ex.ToString()); }
        }
        public string postData()
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    string postUrl = "http://anket.5adim.com/servis/Talepler.php/setTalepler";

                    byte[] gelenYanit = client.UploadValues(postUrl, new NameValueCollection()
                     {
                        { "token",  "e37fd7e3df0fc392e925db0d7a901702"},
                        { "sirketId", db.degiskenler.SirketId }, 
                        { "adSoyad", AdSoyad},
                        { "denekNumara", GelisTipi+ProtokolId }
                     });

                    string result = Encoding.UTF8.GetString(gelenYanit);
                    DonenDegerler obj = JsonConvert.DeserializeObject<DonenDegerler>(result);

                    if (obj.sonuc == "success") return obj.data[0].ToString();
                    else return "00";
                }
            }
            catch (Exception ex) { dosyaIslem(ex.ToString()); throw; }

        }

        public void dosyaIslem(string hata)
        {
            try
            {
                writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\log.txt", true);
                writer.WriteLine(DateTime.Now.ToString() + ":" + hata);
                writer.Flush();
                writer.Close();

              
            }
            catch (Exception) { }
        }
    }
}
