using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Proteomics;

namespace ProteoformSuiteInternal
{
    public class ProteoformFamily
    {

        #region Private Field

        private static int family_counter = 0;

        #endregion Private Field

        #region Private Property

        private Proteoform seed { get; set; }

        #endregion Private Property

        #region Public Property

        public int family_id { get; set; }
        public ProteoformCommunity community { get; set; }
        public string name_list { get { return String.Join("; ", theoretical_proteoforms.Select(p => p.name)); } }
        public string accession_list { get { return String.Join("; ", theoretical_proteoforms.Select(p => p.accession)); } }
        public string gene_list { get { return String.Join("; ", gene_names.Select(p => p.get_prefered_name(ProteoformCommunity.preferred_gene_label)).Where(n => n != null).Distinct()); } }
        public string experimentals_list { get { return String.Join("; ", experimental_proteoforms.Select(p => p.accession)); } }
        public string agg_mass_list { get { return String.Join("; ", experimental_proteoforms.Select(p => Math.Round(p.agg_mass, SaveState.lollipop.deltaM_edge_display_rounding))); } }
        public List<ExperimentalProteoform> experimental_proteoforms { get; private set; }
        public List<TheoreticalProteoform> theoretical_proteoforms { get; private set; }
        public List<GeneName> gene_names { get; private set; }
        public List<ProteoformRelation> relations { get; private set; }
        public List<Proteoform> proteoforms { get; private set; }

        #endregion Public Property

        #region Public Constructor

        public ProteoformFamily(Proteoform seed)
        {
            family_counter++;
            this.family_id = family_counter;
            this.seed = seed;
        }

        #endregion Public Constructor

        #region Public Methods

        public void construct_family()
        {
            proteoforms = new HashSet<Proteoform>(construct_family(new List<Proteoform> { seed })).ToList();
            separate_proteoforms();
        }

        public void merge_families(List<ProteoformFamily> families)
        {
            IEnumerable<ProteoformFamily> gene_family =
                    from f in families
                    from n in gene_names.Select(g => g.get_prefered_name(ProteoformCommunity.preferred_gene_label)).Distinct()
                    where f.gene_names.Select(g => g.get_prefered_name(ProteoformCommunity.preferred_gene_label)).Contains(n)
                    select f;
            proteoforms = new HashSet<Proteoform>(proteoforms.Concat(gene_family.SelectMany(f => f.proteoforms))).ToList();
            separate_proteoforms();
        }

        public void identify_experimentals()
        {
            HashSet<ExperimentalProteoform> identified_experimentals = new HashSet<ExperimentalProteoform>();
            Parallel.ForEach(theoretical_proteoforms, t =>
            {
                lock (identified_experimentals)
                {
                    foreach (ExperimentalProteoform e in t.identify_connected_experimentals(SaveState.lollipop.theoretical_database.all_possible_ptmsets, SaveState.lollipop.theoretical_database.all_mods_with_mass))
                    {
                        identified_experimentals.Add(e);
                    }
                }
            });

            //Continue looking for new experimental identifications until no more remain to be identified
            List<ExperimentalProteoform> newly_identified_experimentals = new List<ExperimentalProteoform>(identified_experimentals);
            int last_identified_count = identified_experimentals.Count - 1;
            while (newly_identified_experimentals.Count > 0 && identified_experimentals.Count > last_identified_count)
            {
                last_identified_count = identified_experimentals.Count;
                HashSet<ExperimentalProteoform> tmp_new_experimentals = new HashSet<ExperimentalProteoform>();
                Parallel.ForEach(newly_identified_experimentals, id_experimental =>
                {
                    lock (identified_experimentals) lock (tmp_new_experimentals)
                    {
                        foreach (ExperimentalProteoform new_e in id_experimental.identify_connected_experimentals(SaveState.lollipop.theoretical_database.all_possible_ptmsets, SaveState.lollipop.theoretical_database.all_mods_with_mass))
                        {
                            identified_experimentals.Add(new_e);
                            tmp_new_experimentals.Add(new_e);
                        }
                    }
                });
                newly_identified_experimentals = new List<ExperimentalProteoform>(tmp_new_experimentals);
            }

            if (!SaveState.lollipop.supplement_theoreticals || community.decoy_database != null)
                return;

            //If identified experimentals have ptmsets represented in the database, create ET connections
            List<ExperimentalProteoform> daisy_chain_identified = identified_experimentals.Where(ep => ep.relationships.Any(r => r.Accepted && (r.RelationType == ProteoformComparison.ExperimentalTheoretical || r.RelationType == ProteoformComparison.ExperimentalDecoy))).ToList();
            foreach (TheoreticalProteoform theo_ref in new HashSet<TheoreticalProteoform>(daisy_chain_identified.Select(ep => ep.linked_proteoform_references.First() as TheoreticalProteoform)))
            {
                Dictionary<int, List<Modification>> positional_theo_mods = new Dictionary<int, List<Modification>>();
                Dictionary<double, List<int>> theo_mod_positions = new Dictionary<double, List<int>>();
                foreach (ProteinWithGoTerms prot in theo_ref.ExpandedProteinList)
                {
                    //Add mods in range of theoretical
                    foreach (var positional_mods in prot.OneBasedPossibleLocalizedModifications)
                    {
                        if (positional_mods.Key < theo_ref.begin || positional_mods.Key > theo_ref.end)
                            continue;

                        if (positional_theo_mods.TryGetValue(positional_mods.Key, out List<Modification> theo_mods)) theo_mods.AddRange(positional_mods.Value);
                        else positional_theo_mods.Add(positional_mods.Key, positional_mods.Value);
                        foreach (ModificationWithMass m in positional_mods.Value.OfType<ModificationWithMass>().ToList())
                        {
                            if (theo_mod_positions.TryGetValue(m.monoisotopicMass, out List<int> positions)) positions.Add(positional_mods.Key);
                            else theo_mod_positions.Add(m.monoisotopicMass, new List<int> { positional_mods.Key });
                        }
                    }

                    //Add variable mods
                    foreach (ModificationWithMass m in SaveState.lollipop.theoretical_database.variableModifications)
                    {
                        for (int i = 1; i <= theo_ref.sequence.Length; i++)
                        {
                            if (prot.BaseSequence[i - 1].ToString() == m.motif.Motif)
                            {
                                if (!positional_theo_mods.TryGetValue(i, out List<Modification> a)) positional_theo_mods.Add(i, new List<Modification> { m });
                                else a.Add(m);
                                if (theo_mod_positions.TryGetValue(m.monoisotopicMass, out List<int> b)) b.Add(i);
                                else theo_mod_positions.Add(m.monoisotopicMass, new List<int> { i });
                            }
                        }
                    }
                }

                foreach (ExperimentalProteoform ep in identified_experimentals)
                {
                    if (ep.linked_proteoform_references.First() != theo_ref) 
                        continue;

                    List<Ptm> ptms =
                        (from m in ep.ptm_set.ptm_combination.Select(ptm => ptm.modification)
                         from position in theo_mod_positions.ContainsKey(m.monoisotopicMass) ? new HashSet<int>(theo_mod_positions[m.monoisotopicMass]) : new HashSet<int>()
                         from theo_m in positional_theo_mods.ContainsKey(position) ? positional_theo_mods[position] : new List<Modification>()
                         where theo_m as ModificationWithMass != null && m.monoisotopicMass == (theo_m as ModificationWithMass).monoisotopicMass
                         select new Ptm(position, theo_m as ModificationWithMass))
                         .ToList();

                    //Not possible to make a theoretical if there aren't enough PTMs
                    if (ptms.Count < ep.ptm_set.ptm_combination.Count || ptms.Count <= 0)
                        continue;

                    HashSet<double> ptm_masses = new HashSet<double>(ep.ptm_set.ptm_combination.Select(ptm => ptm.modification.monoisotopicMass));
                    Dictionary<double, int> neg_mod_ranks = ptm_masses.ToDictionary(mass => mass, mass => -1);
                    List<PtmSet> sets = PtmCombos.unique_positional_combinations(ptms, ep.ptm_set.ptm_combination.Count, neg_mod_ranks, -1).ToList(); //Don't penalize anything in this round
                    List<ModificationWithMass> ep_mods = ep.ptm_set.ptm_combination.Select(ppp => ppp.modification).ToList();

                    //Find the unique positional mod sets with the mods in the identified experimental set
                    List<PtmSet> sets_with_these_mods =
                        sets.Where(set => ep_mods.All(mod =>
                        set.ptm_combination.Count(ppp => ppp.modification == mod) == ep_mods.Count(ep_mod => ep_mod == mod)))
                        .ToList();

                    if (sets_with_these_mods.Count <= 0)
                        continue;

                    //This experimental can be explained by PTMs in the database, so build a theoretical proteoform for it
                    TheoreticalProteoform tp = new TheoreticalProteoform(theo_ref.accession, theo_ref.description, theo_ref.ExpandedProteinList, theo_ref.unmodified_mass, theo_ref.lysine_count, sets_with_these_mods.First(), theo_ref.is_target, false, null);
                    ProteoformRelation et = new ProteoformRelation(ep, tp, tp.is_target ? ProteoformComparison.ExperimentalTheoretical : ProteoformComparison.ExperimentalDecoy, ep.modified_mass - tp.modified_mass);
                    DeltaMassPeak peak = SaveState.lollipop.et_peaks.OrderByDescending(jkl => jkl.grouped_relations.Count).FirstOrDefault(jkl => jkl.DeltaMass - SaveState.lollipop.peak_width_base_et / 2 <= et.DeltaMass && jkl.DeltaMass + SaveState.lollipop.peak_width_base_et / 2 >= et.DeltaMass);
                    et.peak = peak;
                    et.Accepted = peak != null && peak.Accepted;
                    ep.relationships.Add(et);
                    tp.relationships.Add(et);
                    //theoretical_proteoforms.Add(tp); //not adding them to theoreticals, so we can clear them
                    //proteoforms.Add(tp);
                    relations.Add(et);
                    if (community.decoy_database == null) SaveState.lollipop.et_relations.Add(et);
                    else SaveState.lollipop.ed_relations[community.decoy_database].Add(et);
                }
            }

            
        }

        #endregion Public Methods

        #region Private Methods

        private List<Proteoform> construct_family(List<Proteoform> seed)
        {
            List<Proteoform> seed_expansion = seed.SelectMany(p => p.get_connected_proteoforms()).Except(seed).ToList();
            if (seed_expansion.Count == 0) return seed;
            seed.AddRange(seed_expansion);
            return construct_family(seed);
        }

        private void separate_proteoforms()
        {
            theoretical_proteoforms = proteoforms.OfType<TheoreticalProteoform>().ToList();
            gene_names = theoretical_proteoforms.Select(t => t.gene_name).ToList();
            experimental_proteoforms = proteoforms.OfType<ExperimentalProteoform>().ToList();
            relations = new HashSet<ProteoformRelation>(proteoforms.SelectMany(p => p.relationships.Where(r => r.Accepted))).ToList();
        }

        #endregion Private Methods

    }
}
