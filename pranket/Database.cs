using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace prAnket
{
    public class Database
    {
        public Degiskenler degiskenler = new Degiskenler();
        private SqlCommand sqlCommand;
        public SqlConnection sqlConnection; 
        private string connectionString = "";
        string PusulaServer, PusulaSQLDatabase, PusulaSQLAdi, PusulaSQLSifre, SMSKullanici, SMSSifre, PusulaTag;

        public Database()
        {
            xmlParse();
            getConnect();
        }
        public void xmlParse()
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(AppDomain.CurrentDomain.BaseDirectory + "\\ayarlar.xml");
                XmlNodeList xmlNodeList = xmlDoc.GetElementsByTagName("BESADIM"); //Tek tek her item'deki verileri çekiyoruz.

                foreach (XmlNode node in xmlNodeList)
                { 
                    degiskenler.PusulaServer = node["PusulaServer"].InnerText; //item Kısmındaki Ad verisini çekiyoruz
                    degiskenler.PusulaSQLDatabase = node["PusulaSQLDatabase"].InnerText; //item Kısmındaki Soyad verisini çekiyoruz
                    degiskenler.PusulaSQLAdi = node["PusulaSQLAdi"].InnerText; //item Kısmındaki Soyad verisini çekiyoruz
                    degiskenler.PusulaSQLSifre = node["PusulaSQLSifre"].InnerText; //item Kısmındaki WebSitesi verisini çekiyoruz
                    degiskenler.SMSBaslik = node["SMSBaslik"].InnerText;
                    degiskenler.SMSSirket = node["SMSSirket"].InnerText;
                    degiskenler.SMSKullanici = node["SMSKullanici"].InnerText; //item Kısmındaki WebSitesi verisini çekiyoruz
                    degiskenler.SMSKodu = node["SMSKodu"].InnerText; //item Kısmındaki WebSitesi verisini çekiyoruz
                    degiskenler.SMSSifre = node["SMSSifre"].InnerText; //item Kısmındaki WebSitesi verisini çekiyoruz
                    degiskenler.PusulaTag = node["PusulaTag"].InnerText; //item Kısmındaki WebSitesi verisini çekiyoruz 
                    degiskenler.SMSIcerik = node["SMSIcerik"].InnerText;
                    degiskenler.SirketId = node["SirketId"].InnerText;
                }
                connectionString = "Data Source=" + degiskenler.PusulaServer + ";Initial Catalog=" + degiskenler.PusulaSQLDatabase + ";User ID=" + degiskenler.PusulaSQLAdi + ";Password=" + degiskenler.PusulaSQLSifre + ";MultipleActiveResultSets=True; Connection Timeout=59";
            }
            catch (Exception ex) { }
        }

        public void getConnect() { sqlConnection = new SqlConnection(connectionString); }

        public SqlCommand getSql(string sql)
        {
            try
            {
                if (sqlConnection.State != System.Data.ConnectionState.Open) sqlConnection.Open();
                sqlCommand = new SqlCommand(sql, sqlConnection);

                return sqlCommand;
            }
            catch (Exception ex) { return null; }
        }

        public SqlDataReader sonuc(SqlCommand sqlCommand)
        {
            SqlDataReader r= sqlCommand.ExecuteReader(); 
            return r;
        }
        
        public void getConnectClose() { sqlConnection.Close(); }


        public SqlDataReader getHastaTelefonById()
        {
            string sql = "";
            try
            {
                sql = "SELECT  DISTINCT p.[Id],p.[GelisTipiId],p.[AcilisTarihi],p.[KapanisTarihi],h.GSM,h.SabitTel,[Adi]+' '+[Soyadi] as AdSoyad  " +
                " from " + degiskenler.PusulaTag + ".Protokol p LEFT JOIN " + degiskenler.PusulaTag + ".Hasta h on h.Id = p.HastaId WHERE " +
                " (p.KapanisTarihi >=  DATEADD(HOUR, -1, DATEADD(DAY,-1,GETDATE())))" +
                " AND (p.KapanisTarihi <= DATEADD(DAY,-1,GETDATE()))";
                
                return sonuc(getSql(sql));
            }
            catch (Exception ex)
            { }
            return null;

        }
    }
}

