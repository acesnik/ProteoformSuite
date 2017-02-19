using System.Collections.Generic;
using System.Linq;
using Proteomics;

namespace ProteoformSuiteInternal
{
    public class ProteinSequenceGroup : Protein
    {
        public List<string> accessionList { get; set; } // this is the list of accession numbers for all proteins that share the same sequence. the list gets alphabetical order

        public ProteinSequenceGroup(List<Protein> proteins)
            : base(proteins[0].BaseSequence, proteins[0].Accession + "_G" + proteins.Count(), new Dictionary<int, List<Modification>>(), proteins[0].OneBasedBeginPositions, proteins[0].OneBasedEndPositions, proteins[0].BigPeptideTypes, proteins[0].Name, proteins[0].FullName, false, proteins[0].IsContaminant, proteins[0].GoTerms)
        {
            this.accessionList = proteins.Select(p => p.Accession).ToList();
            HashSet<int> all_positions = new HashSet<int>(proteins.SelectMany(p => p.OneBasedPossibleLocalizedModifications.Keys).ToList());
            Dictionary<int, List<Modification>> ptms_by_position = new Dictionary<int, List<Modification>>();
            foreach (int position in all_positions)
                ptms_by_position.Add(position, proteins.Where(p => p.OneBasedPossibleLocalizedModifications.ContainsKey(position)).SelectMany(p => p.OneBasedPossibleLocalizedModifications[position]).ToList());
        }
    }
}
