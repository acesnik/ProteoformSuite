﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProteoformSuiteInternal;
using Proteomics;
using System.IO;
using System.Xml;

namespace Test
{
    [TestFixture]
    class TestCytoscapeScript
    {
        [Test]
        public void nodes_table_gives_meaningful_modified_theoreticals()
        {
            Proteoform p = ConstructorsForTesting.make_a_theoretical();
            ProteoformFamily f = new ProteoformFamily(p);
            f.construct_family();
            string node_table = CytoscapeScript.get_cytoscape_nodes_tsv(new List<ProteoformFamily> { f }, 
                false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                f.theoretical_proteoforms, false, Lollipop.gene_name_labels[1]);
            Assert.True(node_table.Contains(CytoscapeScript.modified_theoretical_label));
            Assert.AreNotEqual(f.theoretical_proteoforms[0].accession, CytoscapeScript.get_proteoform_shared_name(p, Lollipop.node_labels[0], 2));
        }

        [Test]
        public void nodes_table_gives_meaningful_modified_theoreticals2()
        {
            ModificationMotif motif;
            ModificationMotif.TryGetMotif("K", out motif);
            ModificationWithMass m = new ModificationWithMass("oxidation", new Tuple<string, string>("", ""), motif, ModificationSites.K, 1, new Dictionary<string, IList<string>>(), new List<double>(), new List<double>(), "");

            Proteoform p = ConstructorsForTesting.make_a_theoretical();
            ProteoformFamily f = new ProteoformFamily(p);
            f.construct_family();
            string node_table = CytoscapeScript.get_cytoscape_nodes_tsv(new List<ProteoformFamily> { f }, 
                false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                f.theoretical_proteoforms, false, Lollipop.gene_name_labels[1]);
            Assert.True(node_table.Contains(CytoscapeScript.modified_theoretical_label));
            Assert.AreNotEqual(f.theoretical_proteoforms[0].accession, CytoscapeScript.get_proteoform_shared_name(p, Lollipop.node_labels[0], 2));
        }

        [Test]
        public void nodes_table_gives_meaningful_unmodified_theoreticals()
        {
            Proteoform p = ConstructorsForTesting.make_a_theoretical();
            ProteoformFamily f = new ProteoformFamily(p);
            f.construct_family();
            string node_table = CytoscapeScript.get_cytoscape_nodes_tsv(new List<ProteoformFamily> { f }, 
                false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                f.theoretical_proteoforms, false, Lollipop.gene_name_labels[1]);
            Assert.True(node_table.Contains(CytoscapeScript.unmodified_theoretical_label));
            Assert.AreNotEqual(f.theoretical_proteoforms[0].accession, CytoscapeScript.get_proteoform_shared_name(p, Lollipop.node_labels[0], 2));
        }

        [Test]
        public void nodes_table_gives_meaningful_case_insensitive_unmodified_theoreticals()
        {
            ModificationMotif motif;
            ModificationMotif.TryGetMotif("K", out motif);
            string mod_title = "unmodified".ToUpper();
            ModificationWithMass m = new ModificationWithMass(mod_title, new Tuple<string, string>("N/A", mod_title), motif, ModificationSites.K, 0, new Dictionary<string, IList<string>>(), new List<double>(), new List<double>(), "");

            Proteoform p = ConstructorsForTesting.make_a_theoretical();
            ProteoformFamily f = new ProteoformFamily(p);
            f.construct_family();
            string node_table = CytoscapeScript.get_cytoscape_nodes_tsv(new List<ProteoformFamily> { f }, 
                false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                f.theoretical_proteoforms, false, Lollipop.gene_name_labels[1]);
            Assert.True(node_table.Contains(CytoscapeScript.unmodified_theoretical_label));
            Assert.AreNotEqual(f.theoretical_proteoforms[0].accession, CytoscapeScript.get_proteoform_shared_name(p, Lollipop.node_labels[0], 2));
        }

        [Test]
        public void nodes_table_gives_meaningful_experimentals()
        {
            ExperimentalProteoform e = ConstructorsForTesting.ExperimentalProteoform("E1");
            e.agg_intensity = 999.99;
            e.agg_mass = 888.88;
            e.agg_rt = 777.77;
            ProteoformFamily f = new ProteoformFamily(e);
            f.construct_family();
            string node_table = CytoscapeScript.get_cytoscape_nodes_tsv(new List<ProteoformFamily> { f }, 
                false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                f.theoretical_proteoforms, false, Lollipop.gene_name_labels[1]);
            Assert.True(node_table.Contains("E1"));
            Assert.True(node_table.Contains("999.99"));
        }

        [Test]
        public void test_write_families_no_experimentals_which_shouldnt_happen()
        {
            List<ProteoformFamily> f = new List<ProteoformFamily> { new ProteoformFamily(ConstructorsForTesting.make_a_theoretical()) };
            f.First().construct_family();
            string message = CytoscapeScript.write_cytoscape_script(f, f, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            Assert.True(message.Contains("Error"));
        }

        [Test]
        public void test_write_families_regular_display()
        {
            TheoreticalProteoform t = ConstructorsForTesting.make_a_theoretical();
            ExperimentalProteoform e = ConstructorsForTesting.ExperimentalProteoform("E1");
            ProteoformRelation et = new ProteoformRelation(e, t, ProteoformComparison.et, 0);
            et.peak = new DeltaMassPeak(et, new List<ProteoformRelation> { et });
            et.peak.peak_accepted = true;
            e.relationships.Add(et);
            t.relationships.Add(et);
            e.agg_intensity = 999.99;
            e.quant.lightIntensitySum = 444.44m;
            e.quant.heavyIntensitySum = 333.33m;
            e.quant.intensitySum = 777.77m;
            List<ProteoformFamily> f = new List<ProteoformFamily> { new ProteoformFamily(e) };
            f.First().construct_family();
            string message = CytoscapeScript.write_cytoscape_script(f, f, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            Assert.True(File.Exists(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.script_file_prefix + "test" + CytoscapeScript.script_file_extension)));
            Assert.True(File.Exists(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.style_file_prefix + "test" + CytoscapeScript.style_file_extension)));
            Assert.True(File.Exists(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension)));
            Assert.True(File.Exists(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension)));

            //Check that the style xml is all there
            List<string> vizProperties = new List<string>();
            using (XmlReader xml = XmlReader.Create(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.style_file_prefix + "test" + CytoscapeScript.style_file_extension)))
            {
                while (xml.Read())
                {
                    switch (xml.NodeType)
                    {
                        case XmlNodeType.Element:
                            if (xml.Name == "visualProperty") vizProperties.Add(xml.GetAttribute("name"));
                            break;
                    }
                }
            }
            Assert.AreEqual(CytoscapeScript.default_styles.Keys.Where(s => s.StartsWith("NETWORK")).Count(), vizProperties.Where(s => s.StartsWith("NETWORK")).Count());
            Assert.AreEqual(CytoscapeScript.default_styles.Keys.Where(s => s.StartsWith("NODE")).Count(), vizProperties.Where(s => s.StartsWith("NODE")).Count());
            Assert.AreEqual(CytoscapeScript.default_styles.Keys.Where(s => s.StartsWith("EDGE")).Count(), vizProperties.Where(s => s.StartsWith("EDGE")).Count());
            Assert.AreEqual(CytoscapeScript.default_styles.Keys.Where(s => !s.StartsWith("NETWORK") && !s.StartsWith("NODE") && !s.StartsWith("EDGE")).Count(), vizProperties.Where(s => !s.StartsWith("NETWORK") && !s.StartsWith("NODE") && !s.StartsWith("EDGE")).Count());
            Assert.AreEqual(CytoscapeScript.default_styles.Keys.Count, vizProperties.Count);
            Assert.True(message.StartsWith("Finished"));
        }

        [Test]
        public void cytoscape_improper_build_folder()
        {
            TheoreticalProteoform t = ConstructorsForTesting.make_a_theoretical();
            ExperimentalProteoform e = ConstructorsForTesting.ExperimentalProteoform("E1");
            ProteoformRelation et = new ProteoformRelation(e, t, ProteoformComparison.et, 0);
            e.agg_intensity = 999.99;
            e.quant.lightIntensitySum = 444.44m;
            e.quant.heavyIntensitySum = 333.33m;
            e.quant.intensitySum = 777.77m;
            List<ProteoformFamily> f = new List<ProteoformFamily> { new ProteoformFamily(e) };
            f.First().construct_family();
            string message = CytoscapeScript.write_cytoscape_script(f, f, 
                "", "", "test",
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            Assert.False(message.StartsWith("Finished"));
        }

        [Test]
        public void cytoscape_edges_and_nodes_match()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            IEnumerable<TheoreticalProteoform> theoreticals = community.families.SelectMany(f => f.theoretical_proteoforms);
            string edges = CytoscapeScript.get_cytoscape_edges_tsv(community.families,
                Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                theoreticals, false, Lollipop.gene_name_labels[1]);
            string[] lines = edges.Split(new char[] { '\n' });
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i] == "") break;
                string[] line = lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string nodes = CytoscapeScript.get_cytoscape_nodes_tsv(community.families, false,
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], 2, 
                theoreticals, false, Lollipop.gene_name_labels[1]);
            lines = nodes.Split(new char[] { '\n' });
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i] == "") break;
                string[] line = lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(9, shared_pf_names_nodes.Count); //both families
        }

        [Test]
        public void cytoscape_script_from_theoreticals()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            CytoscapeScript.write_cytoscape_script(community.families.SelectMany(f => f.theoretical_proteoforms.Where(t => t.proteinList.Select(p => p.FullName).Contains(TestProteoformFamilies.p1_fullName))).ToArray(), community.families, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            string[] edge_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension));
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < edge_lines.Length; i++)
            {
                if (edge_lines[i] == "") break;
                string[] line = edge_lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string[] node_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension));
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < node_lines.Length; i++)
            {
                if (node_lines[i] == "") break;
                string[] line = node_lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(5, shared_pf_names_nodes.Count); //only the first family
        }

        [Test]
        public void cytoscape_script_from_goterm()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            CytoscapeScript.write_cytoscape_script(new GoTerm[] { TestProteoformFamilies.p1_goterm }, community.families, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            string[] edge_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension));
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < edge_lines.Length; i++)
            {
                if (edge_lines[i] == "") break;
                string[] line = edge_lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string[] node_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension));
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < node_lines.Length; i++)
            {
                if (node_lines[i] == "") break;
                string[] line = node_lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(5, shared_pf_names_nodes.Count); //only the first family
        }

        [Test]
        public void cytoscape_script_from_gotermnumber()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            CytoscapeScript.write_cytoscape_script(new GoTermNumber[] { new GoTermNumber(TestProteoformFamilies.p1_goterm, 0,0,0,0) }, community.families,
                TestContext.CurrentContext.TestDirectory, "", "test",
                false, false, false, false,
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2,
                false, Lollipop.gene_name_labels[1]);
            string[] edge_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension));
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < edge_lines.Length; i++)
            {
                if (edge_lines[i] == "") break;
                string[] line = edge_lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string[] node_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension));
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < node_lines.Length; i++)
            {
                if (node_lines[i] == "") break;
                string[] line = node_lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(9, shared_pf_names_nodes.Count); //both families this time because they all have the same stuff...
        }

        [Test]
        public void cytoscape_script_from_quantValues()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            CytoscapeScript.write_cytoscape_script(community.families.SelectMany(f => f.experimental_proteoforms.Select(ex => ex.quant)).ToArray(), community.families, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false,
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, false, Lollipop.gene_name_labels[1]);
            string[] edge_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension));
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < edge_lines.Length; i++)
            {
                if (edge_lines[i] == "") break;
                string[] line = edge_lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string[] node_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension));
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < node_lines.Length; i++)
            {
                if (node_lines[i] == "") break;
                string[] line = node_lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(9, shared_pf_names_nodes.Count); //both families
        }

        [Test]
        public void cytoscape_script_from_subset_of_families()
        {
            ProteoformCommunity community = TestProteoformFamilies.construct_two_families_with_potentially_colliding_theoreticals();
            Lollipop.proteoform_community = community;
            CytoscapeScript.write_cytoscape_script(new List<ProteoformFamily> { community.families[0] }, community.families, 
                TestContext.CurrentContext.TestDirectory, "", "test", 
                false, false, false, false, 
                CytoscapeScript.color_scheme_names[0], Lollipop.edge_labels[0], Lollipop.node_labels[0], CytoscapeScript.node_label_positions[0], 2, 
                false, Lollipop.gene_name_labels[1]);
            string[] edge_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.edge_file_prefix + "test" + CytoscapeScript.edge_file_extension));
            HashSet<string> shared_pf_names_edges = new HashSet<string>();
            for (int i = 1; i < edge_lines.Length; i++)
            {
                if (edge_lines[i] == "") break;
                string[] line = edge_lines[i].Split(new char[] { '\t' });
                shared_pf_names_edges.Add(line[0]);
                shared_pf_names_edges.Add(line[2]);
            }

            string[] node_lines = File.ReadAllLines(Path.Combine(TestContext.CurrentContext.TestDirectory, CytoscapeScript.node_file_prefix + "test" + CytoscapeScript.node_file_extension));
            HashSet<string> shared_pf_names_nodes = new HashSet<string>();
            for (int i = 1; i < node_lines.Length; i++)
            {
                if (node_lines[i] == "") break;
                string[] line = node_lines[i].Split(new char[] { '\t' });
                shared_pf_names_nodes.Add(line[0]);
            }

            Assert.True(shared_pf_names_nodes.All(name => shared_pf_names_edges.Contains(name)));
            Assert.True(shared_pf_names_edges.All(name => shared_pf_names_nodes.Contains(name)));
            Assert.AreEqual(community.families.First().proteoforms.Count, shared_pf_names_nodes.Count);
        }
    }
}
