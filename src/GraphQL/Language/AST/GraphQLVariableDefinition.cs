﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphQL.Parser.Language.AST
{
    public class GraphQLVariableDefinition : ASTNode
    {
        public object DefaultValue { get; set; }

        public override ASTNodeKind Kind
        {
            get
            {
                return ASTNodeKind.VariableDefinition;
            }
        }

        public GraphQLType Type { get; set; }
        public GraphQLVariable Variable { get; set; }
    }
}
