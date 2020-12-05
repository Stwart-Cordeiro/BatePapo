using System;
using System.Data;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Xml;


namespace BatePapoCliente
{
    public partial class FrmBatePapoCliente : Form
    {

        private string Usuario = "Desconhecido";
        private StreamWriter swEnviado;
        private StreamReader swReceper;
        private TcpClient tcpServer;
        private delegate void Atualizalogcall(string stMgs);
        private delegate void FechaConecaocall(string stMotivo);
        private Thread mgsThread;
        private IPAddress endIP;
        private bool Conecta;
        private DataSet ds;
        private DataTable dt;
        private XmlDocument objxml;


        public FrmBatePapoCliente()
        {
            Application.ApplicationExit += new EventHandler(OnExit);
            InitializeComponent();
        }

        public void OnExit(object sender, EventArgs e)
        {
            if (Conecta == true)
            {
                Conecta = false;
                swEnviado.Close();
                swReceper.Close();
                tcpServer.Close();
            }
        }

        private void btnConectar_Click(object sender, EventArgs e)
        {
            DataRow dr = dt.NewRow();
            dr[0] = txtIP.Text;
            dr[1] = txtUser.Text;
            dr[2] = txtBatePapoLog.Text;
            dt.Rows.Add(dr);
            ds.WriteXml("c:/BatePapo/cliente.xml");

            if (Conecta == false)
            {
                inicio();
            }
            else
            {
                Fecha_Conexao("Desconectado a pedido do usuário.");
            }
        }

        private void inicio()
        {
            try
            {
                endIP = IPAddress.Parse(txtIP.Text);
                tcpServer = new TcpClient();
                tcpServer.Connect(endIP, 2502);

                Conecta = true;

                Usuario = txtUser.Text;

                txtIP.Enabled = false;
                txtUser.Enabled = false;
                txtMsg.Enabled = true;
                btnMgs.Enabled = true;
                btnConectar.Text = "Desconectado";

                swEnviado = new StreamWriter(tcpServer.GetStream());
                swEnviado.WriteLine(txtUser.Text);
                swEnviado.Flush();

                mgsThread = new Thread(new ThreadStart(RecebeMgs));
                mgsThread.Start();
            }
            catch (Exception err)
            {
                MessageBox.Show("Erro : " + err.Message, "Erro na conexão com servidor", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void RecebeMgs()
        {
            try
            {
                swReceper = new StreamReader(tcpServer.GetStream());
                string ConResposta = swReceper.ReadLine();

                if (ConResposta[0] == '1')
                {
                    this.Invoke(new Atualizalogcall(this.Atualiza), new object[] { "Conectado com sucesso!" });
                }
                else
                {
                    string Motivo = "Não Conectado: ";
                    Motivo += ConResposta.Substring(2, ConResposta.Length - 2);
                    this.Invoke(new FechaConecaocall(this.Fecha_Conexao), new object[] { Motivo });

                    return;
                }

                while (Conecta)
                {
                    this.Invoke(new Atualizalogcall(this.Atualiza), new object[] { swReceper.ReadLine() });
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }

        private void Fecha_Conexao(string Motivo)
        {
            txtBatePapoLog.AppendText(Motivo + "\r\n");

            txtIP.Enabled = true;
            txtUser.Enabled = true;
            txtMsg.Enabled = false;
            btnMgs.Enabled = false;
            btnConectar.Text = "Conectado";

            Conecta = false;
            swEnviado.Dispose();
            swReceper.Dispose();
            swEnviado.Close();
            swReceper.Close();
            tcpServer.Close();
        }

        private void Atualiza(string strMensagem)
        {
            txtBatePapoLog.AppendText(strMensagem + "\r\n");
        }

        private void EnviaMgs()
        {
            if (txtMsg.Lines.Length >= 1)
            {
                swEnviado.WriteLine(txtMsg.Text);
                swEnviado.Flush();
                txtMsg.Lines = null;
            }
            txtMsg.Text = "";
        }

        private void FrmBatePapoCliente_Load(object sender, EventArgs e)
        {
           ds = new DataSet();

           try
           {
               ds.ReadXml("c:/BatePapo/cliente.xml");
               dt = ds.Tables["Cliente"];
           }
           catch (FileNotFoundException ex)
           {
               dt = new DataTable("Cliente");
               dt.Columns.Add("ServidorIP");
               dt.Columns.Add("Usuario");
               dt.Columns.Add("Msg");
               ds.Tables.Add(dt);
               throw ex;
           }
            objxml = new XmlDocument();
            string caminho = @"c:/BatePapo/cliente.xml";
            objxml.Load(caminho);

            XmlNodeList node = objxml.SelectNodes("/NewDataSet/Cliente");

            foreach (XmlNode a in node)
            {
                txtIP.Text = a.SelectSingleNode("ServidorIP").InnerText;
                txtUser.Text = a.SelectSingleNode("Usuario").InnerText;
            }
            
        }

        private void btnMgs_Click(object sender, EventArgs e)
        {
            this.EnviaMgs();
        }

        private void txtMsg_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13)
            {
                this.EnviaMgs();
            }
        }

      }
}
