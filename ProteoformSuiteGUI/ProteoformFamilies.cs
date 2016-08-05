using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProteoformSuiteInternal;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Controls;
using System.Windows.Media;
using QuickGraph;

namespace ProteoformSuite
{
    public partial class ProteoformFamilies : Form
    {
        private ElementHost ctrlHost;
        private GraphSharpProteoforms.MainWindow wpfAddressCtrl;
        System.Windows.FontWeight initFontWeight;
        double initFontSize;
        System.Windows.FontStyle initFontStyle;
        System.Windows.Media.SolidColorBrush initBackBrush;
        System.Windows.Media.SolidColorBrush initForeBrush;
        System.Windows.Media.FontFamily initFontFamily;

        //private IBidirectionalGraph<object, IEdge<object>> _graphToVisualize;
        //public IBidirectionalGraph<object, IEdge<object>> GraphToVisualize
        //{
        //    get { return _graphToVisualize; }
        //}

        public ProteoformFamilies()
        {
            //EnsureApplicationResources();
            InitializeComponent();
        }

        private void ProteoformFamilies_Load(object sender, EventArgs e)
        {
            if (Lollipop.proteoform_community.families.Count == 0) Lollipop.proteoform_community.construct_families();
            fill_proteoform_families();

            //splitContainer2.Panel2.Controls.Add(GraphToVisualize);

            ctrlHost = new ElementHost();
            ctrlHost.Dock = DockStyle.Fill;
            splitContainer2.Panel2.Controls.Add(ctrlHost);
            wpfAddressCtrl = new GraphSharpProteoforms.MainWindow();
            wpfAddressCtrl.InitializeComponent();
            ctrlHost.Child = wpfAddressCtrl;

            //wpfAddressCtrl.OnButtonClick +=
            //    new MyControls.MyControl1.MyControlEventHandler(
            //    avAddressCtrl_OnButtonClick);
            //wpfAddressCtrl.Loaded += new RoutedEventHandler(
            //    avAddressCtrl_Loaded);
        }

        //private void CreateGraphToVisualize()
        //{
        //    var g = new BidirectionalGraph<object, IEdge<object>>();

        //    //add the vertices to the graph
        //    string[] vertices = new string[5];
        //    for (int i = 0; i < 5; i++)
        //    {
        //        vertices[i] = i.ToString();
        //        g.AddVertex(vertices[i]);
        //    }

        //    //add some edges to the graph
        //    g.AddEdge(new Edge<object>(vertices[0], vertices[1]));
        //    g.AddEdge(new Edge<object>(vertices[1], vertices[2]));
        //    g.AddEdge(new Edge<object>(vertices[2], vertices[3]));
        //    g.AddEdge(new Edge<object>(vertices[3], vertices[1]));
        //    g.AddEdge(new Edge<object>(vertices[1], vertices[4]));

        //    _graphToVisualize = g;
        //}

        //public static void EnsureApplicationResources()
        //{
        //    if (System.Windows.Application.Current == null)
        //    {
        //        // create the Application object
        //        new System.Windows.Application();

        //        // merge in your application resources
        //        System.Windows.Application.Current.Resources.MergedDictionaries.Add(
        //            System.Windows.Application.LoadComponent(
        //                new Uri("MyLibrary;component/Resources/MyResourceDictionary.xaml",
        //                UriKind.Relative)) as ResourceDictionary);
        //    }
        //}

        //void avAddressCtrl_Loaded(object sender, EventArgs e)
        //{
        //    initBackBrush = (SolidColorBrush)wpfAddressCtrl.MyControl_Background;
        //    initForeBrush = wpfAddressCtrl.MyControl_Foreground;
        //    initFontFamily = wpfAddressCtrl.MyControl_FontFamily;
        //    initFontSize = wpfAddressCtrl.MyControl_FontSize;
        //    initFontWeight = wpfAddressCtrl.MyControl_FontWeight;
        //    initFontStyle = wpfAddressCtrl.MyControl_FontStyle;
        //}

        private void fill_proteoform_families()
        {
            DisplayUtility.FillDataGridView(dgv_proteoform_families, Lollipop.proteoform_community.families);
            format_families_dgv();
        }

        private void format_families_dgv()
        {
            //set column header
            //dgv_proteoform_families.Columns["family_id"].HeaderText = "Light Monoisotopic Mass";
            dgv_proteoform_families.Columns["lysine_count"].HeaderText = "Lysine Count";
            dgv_proteoform_families.Columns["experimental_count"].HeaderText = "Experimental Proteoforms";
            dgv_proteoform_families.Columns["theoretical_count"].HeaderText = "Theoretical Proteoforms";
            dgv_proteoform_families.Columns["relation_count"].HeaderText = "Relation Count";
            dgv_proteoform_families.Columns["accession_list"].HeaderText = "Accessions";
            dgv_proteoform_families.Columns["relations"].Visible = false;
        }

        private void dgv_proteoform_families_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) display_family_members(e.RowIndex, e.ColumnIndex);
        }
        private void dgv_proteoform_families_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0) display_family_members(e.RowIndex, e.ColumnIndex);
        }
        private void display_family_members(int row_index, int column_index)
        {
            ProteoformFamily selected_family = (ProteoformFamily)this.dgv_proteoform_families.Rows[row_index].DataBoundItem;
            if (dgv_proteoform_families.Columns[column_index].Name == "theoretical_count")
            {
                if (selected_family.theoretical_count > 0) 
                {
                    DisplayUtility.FillDataGridView(dgv_proteoform_family_members, selected_family.theoretical_proteoforms);
                    DisplayUtility.FormatTheoreticalProteoformTable(dgv_proteoform_family_members);
                }
                else dgv_proteoform_family_members.Rows.Clear();
            }
            else if (dgv_proteoform_families.Columns[column_index].Name == "experimental_count")
            {
                if (selected_family.experimental_count > 0)
                {
                    DisplayUtility.FillDataGridView(dgv_proteoform_family_members, selected_family.experimental_proteoforms);
                    DisplayUtility.FormatAggregatesTable(dgv_proteoform_family_members);
                }
                else dgv_proteoform_family_members.Rows.Clear();
            }
            else if (dgv_proteoform_families.Columns[column_index].Name == "relation_count")
            {
                if (selected_family.relation_count > 0)
                {
                    DisplayUtility.FillDataGridView(dgv_proteoform_family_members, selected_family.relations);
                    DisplayUtility.FormatRelationsGridView(dgv_proteoform_family_members, false, false);
                }
                else dgv_proteoform_family_members.Rows.Clear();
            }
        }
    }
}
