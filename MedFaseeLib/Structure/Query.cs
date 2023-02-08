using System;
using System.Collections.Generic;
using System.Text;

namespace MedFasee.Structure
{
    public class Query
    {
        public string Id { get; private set; }
        public SystemData System { get; private set; }
        public List<Measurement> Measurements { get; private set; }

        public Query(string id, SystemData system, List<Measurement> measurements) { Id = id; System = system; Measurements = measurements; }


    }
}
