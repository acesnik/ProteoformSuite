﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace PS_0._00
{
    public partial class TheoreticalDatabase : Form
    {
        OpenFileDialog openFileDialog2 = new OpenFileDialog();
        OpenFileDialog openFileDialog3 = new OpenFileDialog();
        DataGridView dgv_database = new DataGridView();
        ProteomeDatabaseReader proteomeDatabaseReader = new ProteomeDatabaseReader();
        Protein[] proteinRawInfo = null;

        public TheoreticalDatabase()
        {
            InitializeComponent();
        }

        private void TheoreticalDatabase_Load(object sender, EventArgs e)
        {
            InitializeOpenFileDialog2();
            InitializeOpenFileDialog3();
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            ckbx_OxidMeth.Checked = false;
            ckbx_Carbam.Checked = true;
            ckbx_Meth_Cleaved.Checked = true;

            btn_NeuCode_Lt.Checked = true;

            nUD_MaxPTMs.Minimum = 0;
            nUD_MaxPTMs.Maximum = 5;
            nUD_MaxPTMs.Value = 3;

            nUD_NumDecoyDBs.Minimum = 0;
            nUD_NumDecoyDBs.Maximum = 50;
            nUD_NumDecoyDBs.Value = 0;

            nUD_MinPeptideLength.Minimum = 0;
            nUD_MinPeptideLength.Maximum = 20;
            nUD_MinPeptideLength.Value = 7;
        }

        private void InitializeOpenFileDialog2()
        {
            // Set the file dialog to filter for graphics files.
            this.openFileDialog2.Filter = "UniProt XML (*.xml)|*.xml";
            // Allow the user to select multiple images.
            this.openFileDialog2.Multiselect = false;
            this.openFileDialog2.Title = "UniProt XML Format Database";
        }

        private void InitializeOpenFileDialog3()
        {
            // Set the file dialog to filter for graphics files.
            this.openFileDialog3.Filter = "UniProt PTM List (*.txt)|*.txt";
            // Allow the user to select multiple images.
            this.openFileDialog3.Multiselect = false;
            this.openFileDialog3.Title = "UniProt PTM List";
        }

        private void FillDataBaseTable(string table)
        {
            BindingSource dgv_DB_BS = new BindingSource();
            dgv_DB_BS.DataSource = GlobalData.theoreticalAndDecoyDatabases.Tables[table];
            dgv_Database.DataSource = dgv_DB_BS;
            dgv_database.AutoGenerateColumns = true;
            dgv_database.DefaultCellStyle.BackColor = System.Drawing.Color.LightGray;
            dgv_database.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.DarkGray;
        }

        private void btn_GetUniProtXML_Click(object sender, EventArgs e)
        {
            DialogResult dr = this.openFileDialog2.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                String uniprotXmlFile = openFileDialog2.FileName;
                try
                {
                    tb_UniProtXML_Path.Text = uniprotXmlFile;
                }
                catch (SecurityException ex)
                {
                    // The user lacks appropriate permissions to read files, discover paths, etc.
                    MessageBox.Show("Security error. Please contact your administrator for details.\n\nError message: " + ex.Message + "\n\n" +
                        "Details (send to Support):\n\n" + ex.StackTrace);
                    tb_UniProtXML_Path.Text = "";
                }
                catch (Exception ex)
                {
                    // Could not load the result file - probably related to Windows file system permissions.
                    MessageBox.Show("Cannot display the file: " + uniprotXmlFile.Substring(uniprotXmlFile.LastIndexOf('\\'))
                        + ". You may not have permission to read the file, or it may be corrupt.\n\nReported error: " + ex.Message);
                    tb_UniProtXML_Path.Text = "";
                }
            }
        }

        private void btn_UniPtPtmList_Click(object sender, EventArgs e)
        {
            DialogResult dr = this.openFileDialog3.ShowDialog();
            if (dr == System.Windows.Forms.DialogResult.OK)
            {
                String file = openFileDialog3.FileName;
                try
                {
                    tb_UniProtPtmList_Path.Text = file;
                }
                catch (SecurityException ex)
                {
                    // The user lacks appropriate permissions to read files, discover paths, etc.
                    MessageBox.Show("Security error. Please contact your administrator for details.\n\nError message: " + ex.Message + "\n\n" +
                        "Details (send to Support):\n\n" + ex.StackTrace);
                }
                catch (Exception ex)
                {
                    // Could not load the result file - probably related to Windows file system permissions.
                    MessageBox.Show("Cannot display the file: " + file.Substring(file.LastIndexOf('\\'))
                        + ". You may not have permission to read the file, or it may be corrupt.\n\nReported error: " + ex.Message);
                }

            }
        }

        private void btn_Make_Databases_Click(object sender, EventArgs e)
        {
            ProteomeDatabaseReader.oldPtmlistFilePath = tb_UniProtPtmList_Path.Text;
            bool oxidizedMethionine = Convert.ToBoolean(ckbx_OxidMeth.Checked);
            bool carbam = Convert.ToBoolean(ckbx_Carbam.Checked);
            bool cleavedMethionine = Convert.ToBoolean(ckbx_Meth_Cleaved.Checked);
            int maxPtms = Convert.ToInt32(nUD_MaxPTMs.Value);
            int numDecoyDatabases = Convert.ToInt32(nUD_NumDecoyDBs.Value);
            int minPeptideLength = Convert.ToInt32(nUD_MinPeptideLength.Value);
            Dictionary<char, double> aaIsotopeMassList = new AminoAcidMasses(oxidizedMethionine, carbam, WhichLysineIsotopeComposition()).AA_Masses;

            BindingList<string> bindinglist = new BindingList<string>();
            BindingSource bindingSource = new BindingSource();
            bindingSource.DataSource = bindinglist;
            cmbx_DisplayWhichDB.DataSource = bindingSource;

            //Read the UniProt-XML and ptmlist
            proteinRawInfo = ProteomeDatabaseReader.ReadUniprotXml(tb_UniProtXML_Path.Text, minPeptideLength, cleavedMethionine).ToArray();
            Dictionary<string, Modification> uniprotModificationTable = proteomeDatabaseReader.ReadUniprotPtmlist();

            //Concatenate a giant protein out of all protein read from the UniProt-XML, and construct target and decoy proteoform databases
            string giantProtein = GetOneGiantProtein(proteinRawInfo, cleavedMethionine);
            processEntries(proteinRawInfo, cleavedMethionine, aaIsotopeMassList, maxPtms, uniprotModificationTable);
            processDecoys(numDecoyDatabases, giantProtein, proteinRawInfo, cleavedMethionine, aaIsotopeMassList, maxPtms, uniprotModificationTable);

            //Add the new proteoform databases to the bindingList, and then display
            foreach (DataTable dt in GlobalData.theoreticalAndDecoyDatabases.Tables)
            {
                bindinglist.Add(dt.TableName);
                //cmbx_DisplayWhichDB.Items.Add(dt.TableName[0].ToString());
            }
            FillDataBaseTable(cmbx_DisplayWhichDB.SelectedItem.ToString());
        }

        static DataTable GenerateProteoformDatabaseDataTable(string title)
        {
            DataTable dt = new DataTable(title);//datatable name goes in parentheses.
            dt.Columns.Add("Accession", typeof(string));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Fragment", typeof(string));
            dt.Columns.Add("Begin", typeof(int));
            dt.Columns.Add("End", typeof(int));
            dt.Columns.Add("Mass", typeof(double));
            dt.Columns.Add("Lysine Count", typeof(int));
            dt.Columns.Add("PTM List", typeof(string));
            dt.Columns.Add("PTM Group Mass", typeof(double));
            dt.Columns.Add("Proteoform Mass", typeof(double));
            return dt;
        }

        static void processEntries(Protein[] proteinRawData, bool methionineCleavage, Dictionary<char, double> aaIsotopeMassList,
            int maxPTMsPerProteoform, Dictionary<string, Modification> uniprotModificationTable)
        {

            DataTable target = GenerateProteoformDatabaseDataTable("target");

            for (int i = 0; i < proteinRawData.Length; i++)
            {
                bool isMetCleaved = (methionineCleavage && proteinRawData[i].Begin == 0 && proteinRawData[i].Sequence.Substring(0, 1) == "M"); // methionine cleavage of N-terminus specified
                int startPosAfterCleavage = Convert.ToInt32(isMetCleaved);
                string seq = proteinRawData[i].Sequence.Substring(startPosAfterCleavage, (proteinRawData[i].Sequence.Length - startPosAfterCleavage));
                EnterTheoreticalProteformFamily(target, seq, proteinRawData[i], proteinRawData[i].Accession, maxPTMsPerProteoform, isMetCleaved, aaIsotopeMassList, uniprotModificationTable);
            }
            GlobalData.theoreticalAndDecoyDatabases.Tables.Add(target);
        }

        static void processDecoys(int numDb, string giantProtein, Protein[] proteinRawData, bool methionineCleavage, Dictionary<char, double> aaIsotopeMassList, 
            int maxPTMsPerProteoform, Dictionary<string, Modification> uniprotModificationTable)
        {
            for (int decoyNumber = 0; decoyNumber < numDb; decoyNumber++)
            {

                DataTable decoy = GenerateProteoformDatabaseDataTable("DecoyDatabase_" + decoyNumber);

                new Random().Shuffle(proteinRawData); //Randomize Order of Protein Array
                for (int i = 0; i < proteinRawData.Length; i++)
                {
                    bool isMetCleaved = (methionineCleavage && proteinRawData[i].Begin == 0 && proteinRawData[i].Sequence.Substring(0, 1) == "M"); // methionine cleavage of N-terminus specified
                    int startPosAfterCleavage = Convert.ToInt32(isMetCleaved);

                    //From the concatenated proteome, cut a decoy sequence of a randomly selected length
                    int hunkLength = proteinRawData[i].Sequence.Length - startPosAfterCleavage;
                    string hunk = giantProtein.Substring(0, hunkLength);
                    giantProtein.Remove(0, hunkLength);
                    EnterTheoreticalProteformFamily(decoy, hunk, proteinRawData[i], proteinRawData[i].Accession + "_DECOY_" + decoyNumber, maxPTMsPerProteoform, isMetCleaved, aaIsotopeMassList, uniprotModificationTable);
                }
                GlobalData.theoreticalAndDecoyDatabases.Tables.Add(decoy);
            }
        }

        static void EnterTheoreticalProteformFamily(DataTable table, string seq, Protein prot, string accession, int maxPTMsPerProteoform, bool isMetCleaved,
            Dictionary<char, double> aaIsotopeMassList, Dictionary<string, Modification> uniprotModificationTable)
        {
            //Calculate the properties of this sequence
            double mass = CalculateProteoformMass(ref aaIsotopeMassList, seq);
            int kCount = seq.Split('K').Length - 1;

            //Initialize a PTM combination list with "unmodified," and then add other PTMs 
            List<OneUniquePtmGroup> aupg = new List<OneUniquePtmGroup>(new OneUniquePtmGroup[] { new OneUniquePtmGroup(0, new List<string>(new string[] { "unmodified" })) });
            bool addPtmCombos = maxPTMsPerProteoform > 0 && prot.PositionsAndPtms.Count() > 0;
            if (addPtmCombos)
            {
                aupg.AddRange(new PtmCombos().combos(maxPTMsPerProteoform, uniprotModificationTable, prot.PositionsAndPtms));
            }

            foreach (OneUniquePtmGroup group in aupg)
            {
                List<string> ptm_list = group.unique_ptm_combinations;
                //if (!isMetCleaved) { MessageBox.Show("PTM Combinations: " + String.Join("; ", ptm_list)); }
                Double ptm_mass = group.mass;
                Double proteoform_mass = mass + group.mass;
                table.Rows.Add(accession, prot.Name, prot.Fragment, prot.Begin + Convert.ToInt32(isMetCleaved), prot.End, mass, kCount, string.Join("; ", ptm_list), ptm_mass, proteoform_mass);
            }
        }

        static double CalculateProteoformMass(ref Dictionary<char, double> aaIsotopeMassList, string pForm)
        {
            double proteoformMass = 18.010565; // start with water
            char[] aminoAcids = pForm.ToCharArray();
            for (int i = 0; i < pForm.Length; i++)
            {
                double aMass = 0;
                try
                {
                    aMass = aaIsotopeMassList[aminoAcids[i]];
                }
                catch
                {
                    //MessageBox.Show("Did not recognize amino acid " + aminoAcids[i] + " while calculating the mass.\nThis will be recorded as mass = 0.");
                    aMass = 0;
                }
                proteoformMass = proteoformMass + aMass;
            }
            return proteoformMass;
        }

        static string GetOneGiantProtein(Protein[] proteins, bool mC)
        {
            StringBuilder giantProtein = new StringBuilder(5000000); // this set-aside is autoincremented to larger values when necessary.
            foreach (Protein protein in proteins)
            {
                string sequence = protein.Sequence;
                bool isMetCleaved = mC && (sequence.Substring(0, 1) == "M");
                int startPosAfterMetCleavage = Convert.ToInt32(isMetCleaved);
                switch (protein.Fragment)
                {
                    case "chain":
                    case "signal peptide":
                    case "propeptide":
                    case "peptide":
                        giantProtein.Append(".");
                        break;
                    default:
                        giantProtein.Append("-");
                        break;
                }
                giantProtein.Append(sequence.Substring(startPosAfterMetCleavage));
            }
            return giantProtein.ToString();
        }

        private string WhichLysineIsotopeComposition()
        {
            string kI;
            if (btn_NaturalIsotopes.Checked)
            {
                kI = "n";
            }
            else if (btn_NeuCode_Lt.Checked)
            {
                kI = "l";
            }
            else  // must be heavy neucode (aka btn_NeuCode_Hv.Checked
            {
                kI = "h";
            }
            return kI;
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
            //don't know how to delete this
        }

        private void cmbx_DisplayWhichDB_SelectedIndexChanged(object sender, EventArgs e)
        {
            FillDataBaseTable(cmbx_DisplayWhichDB.SelectedItem.ToString());
        }
    }
}