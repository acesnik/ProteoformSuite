using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace ProteoformSuiteInternal
{
    public class ProteoformFamily
    {
        public int family_id { get; set; }
        public string name_list { get { return String.Join("; ", theoretical_proteoforms.Select(p => p.name)); } }
        public string accession_list { get { return String.Join("; ", theoretical_proteoforms.Select(p => p.accession)); } }
        public string experimentals_list { get { return String.Join("; ", experimental_proteoforms.Select(p => p.accession)); } }
        public string agg_mass_list { get { return String.Join("; ", experimental_proteoforms.Select(p => Math.Round(p.agg_mass, Lollipop.deltaM_edge_display_rounding))); } }
        public int lysine_count { get; set; } = -1;
        public List<ExperimentalProteoform> experimental_proteoforms { get; set; }
        public int experimental_count { get { return this.experimental_proteoforms.Count; } }
        public List<TheoreticalProteoform> theoretical_proteoforms { get; set; }
        public int theoretical_count { get { return this.theoretical_proteoforms.Count; } }
        public HashSet<ProteoformRelation> relations { get; set; }
        public int relation_count { get { return this.relations.Count; } }
        public HashSet<Proteoform> proteoforms { get; set; }

        public ProteoformFamily(IEnumerable<Proteoform> proteoforms, int family_id, bool merge_experimentals)
        {
            this.family_id = family_id;
            this.proteoforms = new HashSet<Proteoform>(proteoforms);
            HashSet<int> lysine_counts = new HashSet<int>(proteoforms.Select(p => p.lysine_count));
            if (lysine_counts.Count == 1) this.lysine_count = lysine_counts.FirstOrDefault();
            this.theoretical_proteoforms = proteoforms.OfType<TheoreticalProteoform>().ToList();

            if (!merge_experimentals) this.experimental_proteoforms = proteoforms.OfType<ExperimentalProteoform>().ToList();
            else
            {
                this.experimental_proteoforms = new List<ExperimentalProteoform>();
                List<ExperimentalProteoform> experimental_proteoforms = proteoforms.OfType<ExperimentalProteoform>().OrderByDescending(e => e.agg_intensity).ToList();
                while (experimental_proteoforms.Count > 0)
                {
                    ExperimentalProteoform curr = experimental_proteoforms[0];
                    double mass_tolerance = curr.agg_mass / 1000000 * (double)Lollipop.mass_tolerance;
                    double low = curr.agg_mass - mass_tolerance;
                    double high = curr.agg_mass + mass_tolerance;
                    List<ExperimentalProteoform> merge_these = experimental_proteoforms.Where(e => e.agg_mass >= low && e.agg_mass <= high).ToList();
                    this.experimental_proteoforms.Add(new ExperimentalProteoform(merge_these));
                    experimental_proteoforms = experimental_proteoforms.Except(merge_these).ToList();
                }
            }

            this.relations = new HashSet<ProteoformRelation>(proteoforms.SelectMany(p => p.relationships.Where(r => r.peak.peak_accepted)), new RelationComparer());
        }
    }

    public class RelationComparer : IEqualityComparer<ProteoformRelation>
    {
        public bool Equals(ProteoformRelation r1, ProteoformRelation r2)
        {
            return
                r1.connected_proteoforms[0] == r2.connected_proteoforms[1] && r1.connected_proteoforms[1] == r2.connected_proteoforms[0] ||
                r1.connected_proteoforms[0] == r2.connected_proteoforms[0] && r1.connected_proteoforms[1] == r2.connected_proteoforms[1];
        }
        public int GetHashCode(ProteoformRelation r)
        {
            return r.instanceId;
        }
    }
}
