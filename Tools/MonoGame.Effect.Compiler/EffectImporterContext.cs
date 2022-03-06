using Microsoft.Xna.Framework.Content.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;

namespace MonoGame.EffectCompiler
{
    class EffectImporterContext : ContentImporterContext
    {
        ContentBuildLogger _logger;

        public EffectImporterContext(ContentBuildLogger logger) : base()
        {
            _logger = logger;
        }

        public override string IntermediateDirectory { get { throw new NotImplementedException(); } }

        public override string OutputDirectory { get { throw new NotImplementedException(); } }

        public override ContentBuildLogger Logger { get { return _logger; } }

        public override void AddDependency(string filename)
        {
            
        }
    }
}
