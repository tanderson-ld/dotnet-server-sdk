﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

using static LaunchDarkly.Sdk.Internal.JsonConverterHelpers;
using static LaunchDarkly.Sdk.Json.LdJsonConverters;

namespace LaunchDarkly.Sdk.Server.Internal.Model
{
    // We use the explicit one-property-at-a-time API provided by System.Text.Json instead of a
    // reflection-based approach. This is both for the sake of performance and because the schema
    // has some variant behavior that would be hard to represent with purely one-to-one field type
    // mapping.
    //
    // The API is fairly straightforward, but there are a few things we need to keep in mind.
    //
    // - In deserialization, we have to be somewhat tolerant of possible variations in the encoding
    // and not assume that it's exactly the way this SDK would have encoded it. We may be reading
    // JSON that was generated by the LD services (via the Go SDK), or by any of the other SDKs if
    // there is a shared database; some of those may for instance write a JSON null in place of an
    // empty array. Therefore, in any case where a value is optional, we should make sure we're
    // using a reader method that allows nulls. But it is OK for us to fail parsing if we see a
    // null in a place where omitting the value can't possibly make sense: for instance, in an
    // array of flag rules, each rule really does need to be a JSON object and not a null.
    //
    // - This is a high-traffic code path so we want to do things as efficiently as possible.

    internal class FeatureFlagSerialization : JsonConverter<FeatureFlag>
    {
        internal static readonly FeatureFlagSerialization Instance = new FeatureFlagSerialization();
        internal static readonly string[] _requiredProperties = new string[] {"version"};

        public override void Write(Utf8JsonWriter w, FeatureFlag flag, JsonSerializerOptions options)
        {
            w.WriteStartObject();

            w.WriteString("key", flag.Key);
            w.WriteNumber("version", flag.Version);
            w.WriteBoolean("deleted", flag.Deleted);
            w.WriteBoolean("on", flag.On);
            if (flag.SamplingRatio.HasValue)
            {
                w.WriteNumber("samplingRatio", flag.SamplingRatio.Value);
            }

            if (flag.ExcludeFromSummaries)
            {
                w.WriteBoolean("excludeFromSummaries", true);
            }

            w.WriteStartArray("prerequisites");
            foreach (var p in flag.Prerequisites)
            {
                w.WriteStartObject();
                w.WriteString("key", p.Key);
                w.WriteNumber("variation", p.Variation);
                w.WriteEndObject();
            }

            w.WriteEndArray();

            WriteTargets(w, "targets", flag.Targets);
            WriteTargets(w, "contextTargets", flag.ContextTargets);

            w.WriteStartArray("rules");
            foreach (var r in flag.Rules)
            {
                w.WriteStartObject();
                w.WriteString("id", r.Id);
                SerializationHelpers.WriteVariationOrRollout(w, r.Variation, r.Rollout);
                SerializationHelpers.WriteClauses(w, "clauses", r.Clauses);
                w.WriteBoolean("trackEvents", r.TrackEvents);
                w.WriteEndObject();
            }

            w.WriteEndArray();

            w.WriteStartObject("fallthrough");
            SerializationHelpers.WriteVariationOrRollout(w, flag.Fallthrough.Variation, flag.Fallthrough.Rollout);
            w.WriteEndObject();

            WriteIntIfNotNull(w, "offVariation", flag.OffVariation);
            SerializationHelpers.WriteValues(w, "variations", flag.Variations);
            w.WriteString("salt", flag.Salt);
            WriteBooleanIfTrue(w, "trackEvents", flag.TrackEvents);
            WriteBooleanIfTrue(w, "trackEventsFallthrough", flag.TrackEventsFallthrough);
            if (flag.DebugEventsUntilDate.HasValue)
            {
                w.WriteNumber("debugEventsUntilDate", flag.DebugEventsUntilDate.Value.Value);
            }

            w.WriteBoolean("clientSide", flag.ClientSide);

            WriteMigration(ref w, flag.Migration);

            w.WriteEndObject();
        }

        public override FeatureFlag Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string key = null;
            int version = 0;
            bool deleted = false;
            bool on = false;
            ImmutableList<Prerequisite> prerequisites = null;
            ImmutableList<Target> targets = null;
            ImmutableList<Target> contextTargets = null;
            ImmutableList<FlagRule> rules = null;
            string salt = null;
            VariationOrRollout fallthrough = new VariationOrRollout();
            int? offVariation = null;
            ImmutableList<LdValue> variations = null;
            bool trackEvents = false, trackEventsFallthrough = false;
            UnixMillisecondTime? debugEventsUntilDate = null;
            bool clientSide = false;
            bool excludeFromSummaries = false;
            long? samplingRatio = null;
            Migration? migration = null;

            for (var obj = RequireObject(ref reader).WithRequiredProperties(_requiredProperties);
                 obj.Next(ref reader);)
            {
                switch (obj.Name)
                {
                    case "key":
                        key = reader.GetString();
                        break;
                    case "version":
                        version = reader.GetInt32();
                        break;
                    case "deleted":
                        deleted = reader.GetBoolean();
                        break;
                    case "on":
                        on = reader.GetBoolean();
                        break;
                    case "prerequisites":
                        var prereqsBuilder = ImmutableList.CreateBuilder<Prerequisite>();
                        for (var arr = RequireArrayOrNull(ref reader); arr.Next(ref reader);)
                        {
                            prereqsBuilder.Add(ReadPrerequisite(ref reader));
                        }

                        prerequisites = prereqsBuilder.ToImmutable();
                        break;
                    case "targets":
                        var targetsBuilder = ImmutableList.CreateBuilder<Target>();
                        for (var arr = RequireArrayOrNull(ref reader); arr.Next(ref reader);)
                        {
                            targetsBuilder.Add(ReadTarget(ref reader));
                        }

                        targets = targetsBuilder.ToImmutable();
                        break;
                    case "contextTargets":
                        var contextTargetsBuilder = ImmutableList.CreateBuilder<Target>();
                        for (var arr = RequireArrayOrNull(ref reader); arr.Next(ref reader);)
                        {
                            contextTargetsBuilder.Add(ReadTarget(ref reader));
                        }

                        contextTargets = contextTargetsBuilder.ToImmutable();
                        break;
                    case "rules":
                        var rulesBuilder = ImmutableList.CreateBuilder<FlagRule>();
                        for (var arr = RequireArrayOrNull(ref reader); arr.Next(ref reader);)
                        {
                            rulesBuilder.Add(ReadFlagRule(ref reader));
                        }

                        rules = rulesBuilder.ToImmutable();
                        break;
                    case "fallthrough":
                        fallthrough = ReadVariationOrRollout(ref reader);
                        break;
                    case "offVariation":
                        offVariation = GetIntOrNull(ref reader);
                        break;
                    case "variations":
                        variations = SerializationHelpers.ReadValues(ref reader);
                        break;
                    case "salt":
                        salt = reader.GetString();
                        break;
                    case "trackEvents":
                        trackEvents = reader.GetBoolean();
                        break;
                    case "trackEventsFallthrough":
                        trackEventsFallthrough = reader.GetBoolean();
                        break;
                    case "debugEventsUntilDate":
                        var dt = GetLongOrNull(ref reader);
                        debugEventsUntilDate =
                            dt.HasValue ? UnixMillisecondTime.OfMillis(dt.Value) : (UnixMillisecondTime?) null;
                        break;
                    case "clientSide":
                        clientSide = reader.GetBoolean();
                        break;
                    case "samplingRatio":
                        samplingRatio = reader.GetInt64();
                        break;
                    case "excludeFromSummaries":
                        excludeFromSummaries = reader.GetBoolean();
                        break;
                    case "migration":
                        migration = ReadMigration(ref reader);
                        break;
                }
            }

            if (key is null && !deleted)
            {
                throw new JsonException("missing flag key");
            }

            return new FeatureFlag(key, version, deleted, on, prerequisites, targets, contextTargets, rules,
                fallthrough,
                offVariation, variations, salt, trackEvents, trackEventsFallthrough, debugEventsUntilDate, clientSide,
                samplingRatio, excludeFromSummaries, migration);
        }

        internal static Prerequisite ReadPrerequisite(ref Utf8JsonReader r)
        {
            string key = null;
            int variation = 0;
            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "key":
                        key = r.GetString();
                        break;
                    case "variation":
                        variation = r.GetInt32();
                        break;
                }
            }

            return new Prerequisite(key, variation);
        }

        internal static Target ReadTarget(ref Utf8JsonReader r)
        {
            ContextKind? contextKind = null;
            ImmutableList<string> values = null;
            int variation = 0;
            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "contextKind":
                        contextKind = SerializationHelpers.MaybeContextKind(r.GetString());
                        break;
                    case "values":
                        values = SerializationHelpers.ReadStrings(ref r);
                        break;
                    case "variation":
                        variation = r.GetInt32();
                        break;
                    default:
                        r.Skip();
                        break;
                }
            }

            return new Target(contextKind, values, variation);
        }

        internal static void WriteTargets(Utf8JsonWriter w, string propName, IEnumerable<Target> targets)
        {
            w.WriteStartArray(propName);
            foreach (var t in targets)
            {
                w.WriteStartObject();
                SerializationHelpers.MaybeWriteContextKind(w, "contextKind", t.ContextKind);
                w.WriteNumber("variation", t.Variation);
                SerializationHelpers.WriteStrings(w, "values", t.Values);
                w.WriteEndObject();
            }

            w.WriteEndArray();
        }

        internal static FlagRule ReadFlagRule(ref Utf8JsonReader r)
        {
            string id = null;
            int? variation = null;
            Rollout? rollout = null;
            ImmutableList<Clause> clauses = null;
            bool trackEvents = false;
            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "id":
                        id = r.GetString();
                        break;
                    case "variation":
                        variation = GetIntOrNull(ref r);
                        break;
                    case "rollout":
                        rollout = ReadRollout(ref r);
                        break;
                    case "clauses":
                        clauses = SerializationHelpers.ReadClauses(ref r);
                        break;
                    case "trackEvents":
                        trackEvents = r.GetBoolean();
                        break;
                }
            }

            return new FlagRule(variation, rollout, id, clauses, trackEvents);
        }

        internal static VariationOrRollout ReadVariationOrRollout(ref Utf8JsonReader r)
        {
            int? variation = null;
            Rollout? rollout = null;
            for (var obj = RequireObjectOrNull(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "variation":
                        variation = GetIntOrNull(ref r);
                        break;
                    case "rollout":
                        rollout = ReadRollout(ref r);
                        break;
                }
            }

            return new VariationOrRollout(variation, rollout);
        }

        internal static Rollout? ReadRollout(ref Utf8JsonReader r)
        {
            ImmutableList<WeightedVariation> variations = null;
            ContextKind? contextKind = null;
            string bucketBy = null;
            RolloutKind kind = RolloutKind.Rollout;
            int? seed = null;
            if (r.TokenType == JsonTokenType.Null)
            {
                r.Skip();
                return null;
            }

            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "variations":
                        var listBuilder = ImmutableList.CreateBuilder<WeightedVariation>();
                        for (var arr = RequireArrayOrNull(ref r); arr.Next(ref r);)
                        {
                            int variation = 0, weight = 0;
                            bool untracked = false;
                            for (var wvObj = RequireObject(ref r); wvObj.Next(ref r);)
                            {
                                switch (wvObj.Name)
                                {
                                    case "variation":
                                        variation = r.GetInt32();
                                        break;
                                    case "weight":
                                        weight = r.GetInt32();
                                        break;
                                    case "untracked":
                                        untracked = r.GetBoolean();
                                        break;
                                }
                            }

                            listBuilder.Add(new WeightedVariation(variation, weight, untracked));
                        }

                        variations = listBuilder.ToImmutable();
                        break;
                    case "contextKind":
                        contextKind = SerializationHelpers.MaybeContextKind(r.GetString());
                        break;
                    case "bucketBy":
                        bucketBy = r.GetString();
                        break;
                    case "kind":
                        var kindStr = r.GetString();
                        kind = "experiment".Equals(kindStr) ? RolloutKind.Experiment : RolloutKind.Rollout;
                        break;
                    case "seed":
                        seed = GetIntOrNull(ref r);
                        break;
                }
            }

            return new Rollout(kind, contextKind, seed, variations,
                SerializationHelpers.AttrRefOrName(contextKind, bucketBy));
        }

        private Migration ReadMigration(ref Utf8JsonReader r)
        {
            long? checkRatio = null;
            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "checkRatio":
                        checkRatio = r.GetInt64();
                        break;
                }
            }

            return new Migration(checkRatio);
        }

        private void WriteMigration(ref Utf8JsonWriter w, Migration? migration)
        {
            if (!migration.HasValue) return;

            w.WritePropertyName("migration");
            w.WriteStartObject();
            if (migration.Value.CheckRatio.HasValue)
            {
                w.WriteNumber("checkRatio", migration.Value.CheckRatio.Value);
            }

            w.WriteEndObject();
        }
    }

    internal class SegmentSerialization : JsonConverter<Segment>
    {
        internal static readonly SegmentSerialization Instance = new SegmentSerialization();
        internal static readonly string[] _requiredProperties = new string[] { "version" };

        public override void Write(Utf8JsonWriter w, Segment segment, JsonSerializerOptions options)
        {
            w.WriteStartObject();

            w.WriteString("key", segment.Key);
            w.WriteNumber("version", segment.Version);
            w.WriteBoolean("deleted", segment.Deleted);
            SerializationHelpers.WriteStrings(w, "included", segment.Included);
            SerializationHelpers.WriteStrings(w, "excluded", segment.Excluded);
            WriteSegmentTargets(w, "includedContexts", segment.IncludedContexts);
            WriteSegmentTargets(w, "excludedContexts", segment.ExcludedContexts);
            w.WriteString("salt", segment.Salt);

            w.WriteStartArray("rules");
            foreach (var r in segment.Rules)
            {
                w.WriteStartObject();
                SerializationHelpers.WriteClauses(w, "clauses", r.Clauses);
                WriteIntIfNotNull(w, "weight", r.Weight);
                if (r.RolloutContextKind.HasValue)
                {
                    w.WriteString("rolloutContextKind", r.RolloutContextKind.Value.Value);
                }
                if (r.BucketBy.Defined)
                {
                    w.WriteString("bucketBy", r.BucketBy.ToString());
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();

            WriteBooleanIfTrue(w, "unbounded", segment.Unbounded);
            SerializationHelpers.MaybeWriteContextKind(w, "unboundedContextKind", segment.UnboundedContextKind);
            WriteIntIfNotNull(w, "generation", segment.Generation);

            w.WriteEndObject();
        }

        public override Segment Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string key = null;
            int version = 0;
            bool deleted = false;
            ImmutableList<string> included = null, excluded = null;
            ImmutableList<SegmentTarget> includedContexts = null, excludedContexts = null;
            ImmutableList<SegmentRule> rules = null;
            string salt = null;
            bool unbounded = false;
            ContextKind? unboundedContextKind = null;
            int? generation = null;

            for (var obj = RequireObject(ref reader).WithRequiredProperties(_requiredProperties);
                obj.Next(ref reader);)
            {
                switch (obj.Name)
                {
                    case "key":
                        key = reader.GetString();
                        break;
                    case "version":
                        version = reader.GetInt32();
                        break;
                    case "deleted":
                        deleted = reader.GetBoolean();
                        break;
                    case "included":
                        included = SerializationHelpers.ReadStrings(ref reader);
                        break;
                    case "excluded":
                        excluded = SerializationHelpers.ReadStrings(ref reader);
                        break;
                    case "includedContexts":
                        includedContexts = ReadSegmentTargets(ref reader);
                        break;
                    case "excludedContexts":
                        excludedContexts = ReadSegmentTargets(ref reader);
                        break;
                    case "rules":
                        var rulesBuilder = ImmutableList.CreateBuilder<SegmentRule>();
                        for (var arr = RequireArray(ref reader); arr.Next(ref reader);)
                        {
                            rulesBuilder.Add(ReadSegmentRule(ref reader));
                        }
                        rules = rulesBuilder.ToImmutable();
                        break;
                    case "salt":
                        salt = reader.GetString();
                        break;
                    case "unbounded":
                        unbounded = reader.GetBoolean();
                        break;
                    case "unboundedContextKind":
                        unboundedContextKind = SerializationHelpers.MaybeContextKind(reader.GetString());
                        break;
                    case "generation":
                        generation = GetIntOrNull(ref reader);
                        break;
                }
            }
            if (key is null && !deleted)
            {
                throw new JsonException("missing segment key");
            }
            return new Segment(key, version, deleted, included, excluded, includedContexts, excludedContexts,
                rules, salt, unbounded, unboundedContextKind, generation);
        }

        internal static ImmutableList<SegmentTarget> ReadSegmentTargets(ref Utf8JsonReader r)
        {
            var builder = ImmutableList.CreateBuilder<SegmentTarget>();
            for (var arr = RequireArrayOrNull(ref r); arr.Next(ref r);)
            {
                ContextKind? contextKind = null;
                ImmutableList<string> values = null;
                for (var obj = RequireObject(ref r); obj.Next(ref r);)
                {
                    switch (obj.Name)
                    {
                        case "contextKind":
                            contextKind = SerializationHelpers.MaybeContextKind(r.GetString());
                            break;
                        case "values":
                            values = SerializationHelpers.ReadStrings(ref r);
                            break;
                    }
                }
                builder.Add(new SegmentTarget(contextKind, values));
            }
            return builder.ToImmutable();
        }

        internal static void WriteSegmentTargets(Utf8JsonWriter w, string propName, IEnumerable<SegmentTarget> targets)
        {
            w.WriteStartArray(propName);
            foreach (var t in targets)
            {
                w.WriteStartObject();
                if (t.ContextKind.HasValue)
                {
                    w.WriteString("contextKind", t.ContextKind.Value.Value);
                }
                SerializationHelpers.WriteStrings(w, "values", t.Values);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }

        internal static SegmentRule ReadSegmentRule(ref Utf8JsonReader r)
        {
            ImmutableList<Clause> clauses = null;
            int? weight = null;
            ContextKind? rolloutContextKind = null;
            string bucketBy = null;
            for (var obj = RequireObject(ref r); obj.Next(ref r);)
            {
                switch (obj.Name)
                {
                    case "clauses":
                        clauses = SerializationHelpers.ReadClauses(ref r);
                        break;
                    case "weight":
                        weight = GetIntOrNull(ref r);
                        break;
                    case "rolloutContextKind":
                        rolloutContextKind = SerializationHelpers.MaybeContextKind(r.GetString());
                        break;
                    case "bucketBy":
                        bucketBy = r.GetString();
                        break;
                }
            }
            return new SegmentRule(clauses, weight, rolloutContextKind,
                SerializationHelpers.AttrRefOrName(rolloutContextKind, bucketBy));
        }

    }

    internal static class SerializationHelpers
    {
        internal static void WriteVariationOrRollout(Utf8JsonWriter w, int? variation, Rollout? rollout)
        {
            if (variation.HasValue)
            {
                w.WriteNumber("variation", variation.Value);
            }
            if (rollout.HasValue)
            {
                w.WriteStartObject("rollout");
                switch (rollout.Value.Kind)
                {
                    case RolloutKind.Rollout:
                        break; // that's the default, omit the property
                    case RolloutKind.Experiment:
                        w.WriteString("kind", "experiment");
                        break;
                }
                if (rollout.Value.ContextKind.HasValue)
                {
                    w.WriteString("contextKind", rollout.Value.ContextKind.Value.Value);
                }
                WriteIntIfNotNull(w, "seed", rollout.Value.Seed);
                w.WriteStartArray("variations");
                foreach (var v in rollout.Value.Variations)
                {
                    w.WriteStartObject();
                    w.WriteNumber("variation", v.Variation);
                    w.WriteNumber("weight", v.Weight);
                    WriteBooleanIfTrue(w, "untracked", v.Untracked);
                    w.WriteEndObject();
                }
                w.WriteEndArray();
                if (rollout.Value.BucketBy.Defined)
                {
                    w.WriteString("bucketBy", rollout.Value.BucketBy.ToString());
                }
                w.WriteEndObject();
            }
        }

        internal static void WriteClauses(Utf8JsonWriter w, string propName, IEnumerable<Clause> clauses)
        {
            w.WriteStartArray(propName);
            foreach (var c in clauses)
            {
                w.WriteStartObject();
                MaybeWriteContextKind(w, "contextKind", c.ContextKind);
                w.WriteString("attribute", c.Attribute.ToString());
                w.WriteString("op", c.Op.Name);
                WriteValues(w, "values", c.Values);
                w.WriteBoolean("negate", c.Negate);
                w.WriteEndObject();
            }
            w.WriteEndArray();
        }

        internal static void WriteStrings(Utf8JsonWriter w, string propName, IEnumerable<string> values)
        {
            w.WriteStartArray(propName);
            foreach (var v in values)
            {
                w.WriteStringValue(v);
            }
            w.WriteEndArray();
        }

        internal static void WriteValues(Utf8JsonWriter w, string propName, IEnumerable<LdValue> values)
        {
            w.WriteStartArray(propName);
            foreach (var v in values)
            {
                LdValueConverter.WriteJsonValue(v, w);
            }
            w.WriteEndArray();
        }

        internal static ImmutableList<Clause> ReadClauses(ref Utf8JsonReader r)
        {
            var builder = ImmutableList.CreateBuilder<Clause>();
            for (var arr = RequireArrayOrNull(ref r); arr.Next(ref r);)
            {
                ContextKind? contextKind = null;
                string attribute = null;
                Operator op = null;
                ImmutableList<LdValue> values = null;
                bool negate = false;
                for (var obj = RequireObject(ref r); obj.Next(ref r);)
                {
                    switch (obj.Name)
                    {
                        case "contextKind":
                            contextKind = MaybeContextKind(r.GetString());
                            break;
                        case "attribute":
                            attribute = r.GetString();
                            break;
                        case "op":
                            op = Operator.ForName(r.GetString());
                            // Operator.ForName never returns null - unrecognized operators return a stub object
                            break;
                        case "values":
                            values = ReadValues(ref r);
                            break;
                        case "negate":
                            negate = r.GetBoolean();
                            break;
                    }
                }
                builder.Add(new Clause(contextKind, AttrRefOrName(contextKind, attribute), op, values, negate));
            }
            return builder.ToImmutable();
        }

        internal static ContextKind? MaybeContextKind(string s) => s is null ? (ContextKind?)null : ContextKind.Of(s);

        internal static void MaybeWriteContextKind(Utf8JsonWriter w, string propName, ContextKind? kind)
        {
            if (kind.HasValue)
            {
                w.WriteString(propName, kind.Value.Value);
            }
        }

        internal static ImmutableList<string> ReadStrings(ref Utf8JsonReader r)
        {
            var builder = ImmutableList.CreateBuilder<string>();
            for (var arr = RequireArrayOrNull(ref r); arr.Next(ref r);)
            {
                builder.Add(r.GetString());
            }
            return builder.ToImmutable();
        }

        internal static ImmutableList<LdValue> ReadValues(ref Utf8JsonReader r)
        {
            var builder = ImmutableList.CreateBuilder<LdValue>();
            for (var arr = RequireArrayOrNull(ref r); arr.Next(ref r);)
            {
                builder.Add(LdValueConverter.ReadJsonValue(ref r));
            }
            return builder.ToImmutable();
        }

        internal static AttributeRef AttrRefOrName(ContextKind? contextKind, string attrString)
        {
            // If contextKind is specified, then attrString should be interpreted as an attribute reference
            // which could be a slash-delimited path. If contextKind is not specified, then attrString should
            // be interpreted as a literal attribute name only.
            if (string.IsNullOrEmpty(attrString))
            {
                return new AttributeRef();
            }
            if (!contextKind.HasValue)
            {
                return AttributeRef.FromLiteral(attrString);
            }
            return AttributeRef.FromPath(attrString);
        }
    }
}
