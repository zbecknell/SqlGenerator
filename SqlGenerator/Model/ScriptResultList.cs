using System.Collections.Generic;

namespace SqlGenerator.Model
{
    public class ScriptResultList : List<ScriptResult>
    {
        public void CullList(ScriptResult scriptResult)
        {
            foreach (var result in this)
            {
                if (result.Server == scriptResult.Server && result.InputScript == scriptResult.InputScript)
                    Remove(result);
            }
        }
    }
}
