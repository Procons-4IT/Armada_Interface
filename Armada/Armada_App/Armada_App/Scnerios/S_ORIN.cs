﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Armada_Sync;
using System.Collections;
using System.Xml.Linq;

namespace Armada_App
{
    public partial class S_ORIN : Form
    {
        private const string TRANSACTIONLOG = "Armada_Service_M_S_ORIN_s";
        private DataTable oDtTransLog = null;
        private string sQuery = string.Empty;
        private DataTable oCompDT = null;
        private string strCompany = string.Empty;
        private string strWareHouse = string.Empty;
        private SAPbobsCOM.Company oGetCompany = null;
        BackgroundWorker m_oWorker;
        private DataTable oCompanyDT = null;
        private DataTable oShopDT = null;
        private string strShopXML = "";

        public S_ORIN()
        {
            InitializeComponent();
            m_oWorker = new BackgroundWorker();
            m_oWorker.DoWork += new DoWorkEventHandler(m_oWorker_DoWork);
            m_oWorker.ProgressChanged += new ProgressChangedEventHandler
                    (m_oWorker_ProgressChanged);
            m_oWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (m_oWorker_RunWorkerCompleted);
            m_oWorker.WorkerReportsProgress = true;
            m_oWorker.WorkerSupportsCancellation = true;
        }

        void m_oWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                StatusLabel.Text = "Sync Process Completed...";
                LoadAll();
                btnFilter.Enabled = true;
            }
            catch (Exception ex)
            {
                TransLog.traceService(ex.StackTrace.ToString());
                TransLog.traceService(ex.Message.ToString());
            }
        }

        void m_oWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            toolStripProgressBar1.Value = e.ProgressPercentage;
        }

        void m_oWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                string strInterface = System.Configuration.ConfigurationManager.AppSettings["InterDB"].ToString();
                string strMainDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
                for (int i = 0; i < dgv_S_ORIN.RowCount; i++)
                {
                    try
                    {
                        bool blnSync = false;
                        Hashtable htCCDet = new Hashtable();
                        string[] strValues = new string[4];

                        sQuery = " Select T1.U_COMPANY,T0.U_WAREHOUSE,T1.U_OUTACCT,ISNULL(T0.U_COSTCEN,'') As U_COSTCEN From  ";
                        sQuery += " [@Z_INBOUNDMAPPINGC] T0 JOIN [@Z_INBOUNDMAPPING] T1 On T0.Code = T1.Code ";
                        sQuery += " Where T0.U_SHOPID = '" + dgv_S_ORIN.Rows[i].Cells["ShopID"].Value.ToString() + "'";
                        oCompDT = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(sQuery, strMainDB);
                        if (oCompDT != null)
                        {
                            if (oCompDT.Rows.Count > 0)
                            {
                                strCompany = oCompDT.Rows[0][0].ToString();
                                strWareHouse = oCompDT.Rows[0][1].ToString();
                                oGetCompany = TransLog.GetCompany(strCompany);
                                strValues[0] = oCompDT.Rows[0][2].ToString();
                                strValues[1] = oCompDT.Rows[0][3].ToString();
                            }
                        }

                        TransLog.traceService(" Transaction Type : " + dgv_S_ORIN.Rows[i].Cells["Scenario"].Value.ToString().Trim());
                        TransLog.traceService(" Transaction Key : " + dgv_S_ORIN.Rows[i].Cells["Source_Key"].Value.ToString().Trim());
                        if (oGetCompany != null)
                        {
                            TransLog.traceService("Company DB : " + oGetCompany.CompanyDB);
                            if (oGetCompany.Connected)
                            {
                                Singleton.obj_S_ORIN.Sync((dgv_S_ORIN.Rows[i].Cells["Source_Key"].Value.ToString().Trim()), TransType.A, oGetCompany, strInterface, strWareHouse, strValues, htCCDet);

                                string strQuery = "Select Status,Remarks From dbo.Z_OTXN Where Scenario = '" + dgv_S_ORIN.Rows[i].Cells["Scenario"].Value.ToString().Trim() + "' ";
                                strQuery += " And S_DocNo = '" + dgv_S_ORIN.Rows[i].Cells["Source_Key"].Value.ToString().Trim() + "' ";
                                DataTable oStatus = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(strQuery, strInterface);
                                if (oStatus != null && oStatus.Rows.Count > 0)
                                {
                                    if (oStatus.Rows[0][0].ToString() == "1")
                                    {
                                        blnSync = true;
                                        dgv_S_ORIN.Rows[i].Cells["Remarks"].Value = string.Empty;
                                    }
                                    else
                                    {
                                        blnSync = false;
                                        dgv_S_ORIN.Rows[i].Cells["Remarks"].Value = oStatus.Rows[0][1].ToString();
                                    }
                                }
                                else
                                {
                                    blnSync = false;
                                }
                            }
                            else
                            {
                                TransLog.traceService(" Error : Company Not Connected.");
                            }
                        }
                        else
                            TransLog.traceService(" Error : Company Not Found.");

                        if (blnSync)
                        {
                            Image image = Armada_App.Properties.Resources.Yes1;
                            dgv_S_ORIN.Rows[i].Cells["Image"].Value = image;
                        }
                        else
                        {
                            Image image = Armada_App.Properties.Resources.Error1;
                            dgv_S_ORIN.Rows[i].Cells["Image"].Value = image;
                        }
                    }
                    catch (Exception ex)
                    {
                        TransLog.traceService(ex.StackTrace.ToString());
                        TransLog.traceService(ex.Message.ToString());
                    }
                }
                m_oWorker.CancelAsync();
            }
            catch (Exception ex)
            {
                TransLog.traceService(ex.StackTrace.ToString());
                TransLog.traceService(ex.Message.ToString());
            }
            finally
            {
                toolStripProgressBar1.Value = 0;
            }
        }

        private void S_ORIN_Load(object sender, EventArgs e)
        {
            try
            {
                UXUTIL.clsUtilities.setAllControlsThemes(this);
                this.WindowState = FormWindowState.Maximized;
                loadCompanyAndBranchList();
                //LoadAll();
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }            
        }

        private void LoadAll()
        {
            try
            {
                string strInterface = System.Configuration.ConfigurationManager.AppSettings["InterDB"].ToString();
                DataTable oDS_M_S_ORIN = null;
                DateTime oFDate = Fromdate.Value;
                DateTime oTDate = ToDate.Value;
                LoadShopXML();
                string str_M_S_ORIN = "Exec Armada_Service_M_S_ORIN_s '" + oFDate.ToString("yyyyMMdd") + "','" + oTDate.ToString("yyyyMMdd") + "','" + strShopXML + "'";
                oDS_M_S_ORIN = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(str_M_S_ORIN, strInterface);
                dgv_S_ORIN.DataSource = oDS_M_S_ORIN;
                loadError();
            }
            catch (Exception ex)
            {   
                TransLog.traceService(" Error : " + ex.Message);
            }            
        }

        private void loadError()
        {
            try
            {
                for (int i = 0; i < dgv_S_ORIN.RowCount; i++)
                {
                    if (dgv_S_ORIN.Rows[i].Cells[1].Value.ToString() == "-1")
                    {
                        Image image = Armada_App.Properties.Resources.Create1;
                        dgv_S_ORIN.Rows[i].Cells["Image"].Value = image;
                    }
                    else
                    {
                        Image image = Armada_App.Properties.Resources.Red_mark;
                        dgv_S_ORIN.Rows[i].Cells["Image"].Value = image;
                    }
                }
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }
        }

        private void btnSync_Click(object sender, EventArgs e)
        {
            try
            {
                btnFilter.Enabled = false;
                m_oWorker.RunWorkerAsync();    
            }
            catch (Exception ex)
            {
                TransLog.traceService("Error Message : " + ex.Message);
                TransLog.traceService(" Error Source : " + ex.Source);
                MessageBox.Show(ex.Message.ToString());
            }          
        }       

        private void btnCancel_Click(object sender, EventArgs e)
        {
            S_ORIN.ActiveForm.Close();
        }

        private void cmbCompany_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbCompany.SelectedIndex >= 0)
            {
                filterShops(cmbCompany.SelectedValue.ToString());
            }
        }

        private void loadCompanyAndBranchList()
        {
            try
            {

                string strMainDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
                sQuery = " Select T1.U_COMPANY As 'Company' From  ";
                sQuery += " [@Z_INBOUNDMAPPING] T1 Where ISNULL(T1.U_COMPANY,'') <> '' ";
                //sQuery += " Where T0.U_SHOPID = '" + dgv_S_OINV.Rows[i].Cells["ShopID"].Value.ToString().Trim() + "'";               
                oCompanyDT = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(sQuery, strMainDB);
                              
                sQuery = " Select T1.U_COMPANY As 'Company' ,T0.U_SHOPID As 'Shop' From  ";
                sQuery += " [@Z_INBOUNDMAPPINGC] T0 JOIN [@Z_INBOUNDMAPPING] T1 On T0.Code = T1.Code ";
                oShopDT = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(sQuery, strMainDB);

                cmbCompany.DataSource = oCompanyDT;
                cmbCompany.DisplayMember = "Company";
                cmbCompany.ValueMember = "Company";
                cmbCompany.SelectedIndex = 0;
                filterShops(cmbCompany.SelectedValue.ToString());
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }
        }

        private void filterShops(string strCompany)
        {
            try
            {
                //bool Checked = false;
                DataView dv = new DataView();
                dv = oShopDT.DefaultView;
                DataTable dt = dv.Table;
                dt.DefaultView.RowFilter = "Company = '" + strCompany + "'";
                chkShopID.Items.Clear();
                foreach (DataRow dr in dt.DefaultView.ToTable().Rows)
                {
                    chkShopID.Items.Add(dr["Shop"], false);
                }               

                chkShopID.DisplayMember = "Shop";
                chkShopID.ValueMember = "Shop";
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }
        }

        private void LoadShopXML()
        {
            try
            {
                int arrcnt = 0;
                string Shop = null;
                string Code = null;
                string[] ShopArr = new string[chkShopID.CheckedItems.Count];
                for (arrcnt = 0; arrcnt <= chkShopID.CheckedItems.Count - 1; arrcnt++)
                {
                    ShopArr[arrcnt] = chkShopID.CheckedItems[arrcnt].ToString();
                }
                Shop = "Shop";
                Code = "ShopID";
                strShopXML = ShopStringArrayToXML(ShopArr, Shop, Code);
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }

        }

        private String ShopStringArrayToXML(String[] Array, String Element, String Attribute)
        {
            XElement identity = new XElement(Element);
            try
            {
                for (int i = 0; i <= Array.Length - 1; i++)
                {
                    if (Array[i] != null)
                    {
                        XElement elm = new XElement(Attribute, Array[i].Trim());
                        identity.Add(elm);
                    }
                }
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }

            return identity.ToString();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            try
            {
                LoadAll();
            }
            catch (Exception ex)
            {
                TransLog.traceService(" Error : " + ex.Message);
            }
        }
    }
}


//string strInterface = System.Configuration.ConfigurationManager.AppSettings["InterDB"].ToString();
//                string strMainDB = System.Configuration.ConfigurationManager.AppSettings["MainDB"].ToString();
//                oDtTransLog = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(TRANSACTIONLOG, strInterface);
//                if (oDtTransLog != null && oDtTransLog.Rows.Count > 0)
//                {                   
//                    foreach (DataRow dr in oDtTransLog.Rows)
//                    {
//                        try
//                        {
//                            Hashtable htCCDet = new Hashtable();
//                            string[] strValues = new string[4];

//                            sQuery = " Select T1.U_COMPANY,T0.U_WAREHOUSE,T1.U_OUTACCT From  ";
//                            sQuery += " [@Z_INBOUNDMAPPINGC] T0 JOIN [@Z_INBOUNDMAPPING] T1 On T0.Code = T1.Code ";
//                            sQuery += " Where T0.U_SHOPID = '" + dr["ShopID"].ToString() + "'";
//                            oCompDT = Armada_Sync.Singleton.objSqlDataAccess.ExecuteReader(sQuery, strMainDB);
//                            if (oCompDT != null)
//                            {
//                                if (oCompDT.Rows.Count > 0)
//                                {
//                                    strCompany = oCompDT.Rows[0][0].ToString();
//                                    strWareHouse = oCompDT.Rows[0][1].ToString();
//                                    oGetCompany = TransLog.GetCompany(strCompany);
//                                    strValues[0] = oCompDT.Rows[0][2].ToString();
//                                }
//                            }
                            
//                            TransLog.traceService(" Transaction Type : " + dr["Scenario"].ToString());
//                            TransLog.traceService(" Transaction Key : " + dr["Key"].ToString());
//                            if (oGetCompany != null)
//                            {
//                                TransLog.traceService("Company DB : " + oGetCompany.CompanyDB);
//                                if (oGetCompany.Connected)
//                                {
//                                    Singleton.obj_S_ORIN.Sync((dr["Key"].ToString()), TransType.A, oGetCompany, strInterface, strWareHouse, strValues, htCCDet);
//                                }
//                                else
//                                {
//                                    TransLog.traceService(" Error : Company Not Connected.");
//                                }
//                            }
//                            else
//                                TransLog.traceService(" Error : Company Not Found.");
//                        }
//                        catch (Exception ex)
//                        {
//                            TransLog.traceService(" Error : " + ex.Message);
//                        }                        
//                    }
//                }
//                MessageBox.Show("Manual Sync Completed...");
//                LoadAll();