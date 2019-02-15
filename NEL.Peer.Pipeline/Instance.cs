using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Pipeline
{
    public class PipelineSystem
    {
        public static ISystem CreatePipelineSystemV1(NEL.Common.ILogger logger)
        {
            return new PipelineSystemV1(logger);
        }
    }
}
