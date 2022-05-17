﻿using LaunchDarkly.Sdk.Server.Internal.Model;

namespace LaunchDarkly.Sdk.Server.Internal.Evaluation
{
    internal partial struct EvalScope
    {
        private bool MatchClause(in Clause clause)
        {
            // A clause matches if ANY of its values match, for the given attribute and operator
            if (clause.Op == Operator.SegmentMatch)
            {
                foreach (var value in clause.Values)
                {
                    Segment segment = _parent.SegmentGetter(value.AsString);
                    if (segment != null && MatchSegment(segment))
                    {
                        return MaybeNegate(clause, true);
                    }
                }
                return MaybeNegate(clause, false);
            }
            else
            {
                return MatchClauseNoSegments(clause);
            }
        }

        private bool MatchClauseNoSegments(in Clause clause)
        {
            if (!clause.Attribute.Defined)
            {
                return false; // TODO: error
            }
            if (!clause.Attribute.Valid)
            {
                return false; // TODO: error
            }
            if (clause.Attribute.Depth == 1 &&
                clause.Attribute.TryGetComponent(0, out var pathComponent) &&
                pathComponent.Name == "kind")
            {
                return MaybeNegate(clause, MatchClauseByKind(clause));
            }
            if (!_context.TryGetContextByKind(clause.ContextKind ?? Context.DefaultKind, out var matchContext))
            {
                return false;
            }
            var contextValue = matchContext.GetValue(clause.Attribute);
            if (contextValue.IsNull)
            {
                return false; // if the attribute is null/missing, it's an automatic non-match - regardless of Negate
            }
            if (contextValue.Type == LdValueType.Array)
            {
                var list = contextValue.AsList(LdValue.Convert.Json);
                foreach (var element in list)
                {
                    if (element.Type == LdValueType.Array || element.Type == LdValueType.Object)
                    {
                        Logger.Error("Invalid custom attribute value in user object: {0}",
                            element);
                        return false;
                    }
                    if (ClauseMatchAny(clause, element))
                    {
                        return MaybeNegate(clause, true);
                    }
                }
                return MaybeNegate(clause, false);
            }
            else if (contextValue.Type == LdValueType.Object)
            {
                Logger.Warn("Got unexpected user attribute type {0} for user attribute \"{1}\"",
                    contextValue.Type,
                    clause.Attribute);
                return false;
            }
            else
            {
                return MaybeNegate(clause, ClauseMatchAny(clause, contextValue));
            }
        }

        private bool MatchClauseByKind(in Clause clause)
        {
            // If Attribute is "kind", then we treat Operator and Values as a match expression against a list
            // of all individual kinds in the context. That is, for a multi-kind context with kinds of "org"
            // and "user", it is a match if either of those strings is a match with Operator and Values.
            if (_context.Multiple)
            {
                foreach (var individualContext in _context.MultiKindContexts)
                {
                    if (ClauseMatchAny(clause, LdValue.Of(individualContext.Kind)))
                    {
                        return true;
                    }
                }
            }
            return ClauseMatchAny(clause, LdValue.Of(_context.Kind));
        }

        private static bool MaybeNegate(in Clause clause, bool b) =>
            clause.Negate ? !b : b;

        internal static bool ClauseMatchAny(in Clause clause, in LdValue contextValue) // exposed for testing
        {
            // Special case for the "in" operator - we preprocess the values to a set for fast lookup
            if (clause.Op == Operator.In && clause.Preprocessed.ValuesAsSet != null)
            {
                return clause.Preprocessed.ValuesAsSet.Contains(contextValue);
            }

            int index = 0;
            foreach (var clauseValue in clause.Values)
            {
                var preprocessedValue = clause.Preprocessed.Values?[index++];
                if (clause.Op.Apply(contextValue, clauseValue, preprocessedValue))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
