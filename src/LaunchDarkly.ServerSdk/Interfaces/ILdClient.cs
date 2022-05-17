﻿
namespace LaunchDarkly.Sdk.Server.Interfaces
{
    /// <summary>
    /// Interface defining the public methods of <see cref="LdClient"/>.
    /// </summary>
    /// <remarks>
    /// See also <see cref="ILdClientExtensions"/>, which provides convenience methods that build upon
    /// this interface.
    /// </remarks>
    public interface ILdClient
    {
        /// <summary>
        /// A mechanism for tracking the status of a Big Segment store.
        /// </summary>
        /// <remarks>
        /// The returned object has methods for checking whether the Big Segment store is (as far as the SDK
        /// knows) currently operational and tracking changes in this status. See
        /// <see cref="IBigSegmentStoreStatusProvider"/> for more about this functionality.
        /// </remarks>
        IBigSegmentStoreStatusProvider BigSegmentStoreStatusProvider { get; }

        /// <summary>
        /// A mechanism for tracking the status of the data source.
        /// </summary>
        /// <remarks>
        /// The data source is the mechanism that the SDK uses to get feature flag configurations, such as a
        /// streaming connection (the default) or poll requests. The <see cref="IDataSourceStatusProvider"/>
        /// has methods for checking whether the data source is (as far as the SDK knows) currently operational,
        /// and tracking changes in this status. This property will never be null.
        /// </remarks>
        IDataSourceStatusProvider DataSourceStatusProvider { get; }

        /// <summary>
        /// A mechanism for tracking the status of a persistent data store.
        /// </summary>
        /// <remarks>
        /// The <see cref="IDataStoreStatusProvider"/> has methods for checking whether the data store is (as
        /// far as the SDK knows) currently operational and tracking changes in this status. These are only
        /// relevant for a persistent data store; if you are using an in-memory data store, then this property
        /// is a stub object that always reports the store as operational.
        /// </remarks>
        IDataStoreStatusProvider DataStoreStatusProvider { get; }

        /// <summary>
        /// A mechanism for tracking changes in feature flag configurations.
        /// </summary>
        /// <remarks>
        /// The <see cref="IFlagTracker"/> contains methods for requesting notifications about feature flag
        /// changes using an event listener model.
        /// </remarks>
        IFlagTracker FlagTracker { get; }

        /// <summary>
        /// Tests whether the client is ready to be used.
        /// </summary>
        /// <value>true if the client is ready, or false if it is still initializing</value>
        bool Initialized { get; }

        /// <summary>
        /// Calculates the integer value of a feature flag for a given context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag variation has a numeric value that is not an integer, it is rounded to the
        /// nearest integer. This rounding behavior may be changed in a future version of the SDK (for
        /// instance, to round toward zero like the usual float-to-int conversion in C#), so you should
        /// avoid relying on it.
        /// </para>
        /// <para>
        /// If the flag variation does not have a numeric value, <c>defaultValue</c> is returned.
        /// </para>
        /// <para>
        /// If an error makes it impossible to evaluate the flag (for instance, the feature flag key
        /// does not match any existing flag), <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        int IntVariation(string key, Context context, int defaultValue);

        /// <summary>
        /// Calculates the integer value of a feature flag for a given context, and returns an object that
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="IntVariation"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        EvaluationDetail<int> IntVariationDetail(string key, Context context, int defaultValue);

        /// <summary>
        /// Calculates the single-precision floating-point numeric value of a feature flag for a
        /// given context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag variation does not have a numeric value, <c>defaultValue</c> is returned.
        /// </para>
        /// <para>
        /// If an error makes it impossible to evaluate the flag (for instance, the feature flag key
        /// does not match any existing flag), <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        /// <seealso cref="FloatVariationDetail(string, Context, float)"/>
        /// <seealso cref="DoubleVariation(string, Context, double)"/>
        float FloatVariation(string key, Context context, float defaultValue);

        /// <summary>
        /// Calculates the single-precision floating-point numeric value of a feature flag for a
        /// given context, and returns an object that describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="FloatVariation"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        /// <seealso cref="FloatVariation(string, Context, float)"/>
        /// <seealso cref="DoubleVariationDetail(string, Context, double)"/>
        EvaluationDetail<float> FloatVariationDetail(string key, Context context, float defaultValue);

        /// <summary>
        /// Calculates the double-precision floating-point numeric value of a feature flag for a
        /// given context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag variation does not have a numeric value, <c>defaultValue</c> is returned.
        /// </para>
        /// <para>
        /// If an error makes it impossible to evaluate the flag (for instance, the feature flag key
        /// does not match any existing flag), <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        /// <seealso cref="DoubleVariationDetail(string, Context, double)"/>
        /// <seealso cref="FloatVariation(string, Context, float)"/>
        double DoubleVariation(string key, Context context, double defaultValue);

        /// <summary>
        /// Calculates the double-precision floating-point numeric value of a feature flag for a
        /// given context, and returns an object that describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="FloatVariation"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        /// <seealso cref="DoubleVariation(string, Context, double)"/>
        /// <seealso cref="FloatVariationDetail(string, Context, float)"/>
        EvaluationDetail<double> DoubleVariationDetail(string key, Context context, double defaultValue);

        /// <summary>
        /// Calculates the value of a feature flag for a given context as any JSON value type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The type <see cref="LdValue"/> is used to represent any of the value types that can
        /// exist in JSON. Use <see cref="LdValue"/> methods to examine its type and value.
        /// </para>
        /// <para>
        /// If an error makes it impossible to evaluate the flag (for instance, the feature flag key
        /// does not match any existing flag), <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        LdValue JsonVariation(string key, Context context, LdValue defaultValue);

        /// <summary>
        /// Calculates the value of a feature flag for a given context as any JSON value type, and
        /// returns an object that describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="JsonVariationDetail(string, Context, LdValue)"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        EvaluationDetail<LdValue> JsonVariationDetail(string key, Context context, LdValue defaultValue);

        /// <summary>
        /// Calculates the string value of a feature flag for a given context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag variation does not have a string value, <c>defaultValue</c> is returned.
        /// </para>
        /// <para>
        /// Normally, the string value of a flag should not be null, since the LaunchDarkly UI
        /// does not allow you to assign a null value to a flag variation. However, since it may be
        /// possible to create a feature flag with a null variation by other means, and also since
        /// <c>defaultValue</c> is nullable, you should assume that the return value might be null.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        string StringVariation(string key, Context context, string defaultValue);

        /// <summary>
        /// Calculates the string value of a feature flag for a given context, and returns an object that
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="StringVariation"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        EvaluationDetail<string> StringVariationDetail(string key, Context context, string defaultValue);

        /// <summary>
        /// Calculates the boolean value of a feature flag for a given context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If the flag variation does not have a boolean value, <c>defaultValue</c> is returned.
        /// </para>
        /// <para>
        /// If an error makes it impossible to evaluate the flag (for instance, the feature flag key
        /// does not match any existing flag), <c>defaultValue</c> is returned.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>the variation for the given context, or <c>defaultValue</c> if the flag cannot
        /// be evaluated</returns>
        bool BoolVariation(string key, Context context, bool defaultValue = false);

        /// <summary>
        /// Calculates the boolean value of a feature flag for a given context, and returns an object that
        /// describes the way the value was determined.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The <see cref="EvaluationDetail{T}.Reason"/> property in the result will also be included
        /// in analytics events, if you are capturing detailed event data for this flag.
        /// </para>
        /// <para>
        /// The behavior is otherwise identical to <see cref="BoolVariation"/>.
        /// </para>
        /// </remarks>
        /// <param name="key">the unique feature key for the feature flag</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="defaultValue">the default value of the flag</param>
        /// <returns>an <see cref="EvaluationDetail{T}"/> object</returns>
        EvaluationDetail<bool> BoolVariationDetail(string key, Context context, bool defaultValue);

        /// <summary>
        /// Registers the context.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method simply creates an analytics event containing the context properties, to
        /// that LaunchDarkly will know about that context if it does not already.
        /// </para>
        /// <para>
        /// Calling any evaluation method, such as <see cref="BoolVariation(string, Context, bool)"/>,
        /// also sends the context information to LaunchDarkly (if events are enabled), so you only
        /// need to use <see cref="Identify(Context)"/> if you want to identify the context without
        /// evaluating a flag.
        /// </para>
        /// <para>
        /// Note that event delivery is asynchronous, so the event may not actually be sent until
        /// later; see <see cref="LdClient.Flush"/>.
        /// </para>
        /// </remarks>
        /// <param name="context">the evaluation context</param>
        void Identify(Context context);

        /// <summary>
        /// Tracks that an application-defined event occurred.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method creates a "custom" analytics event containing the specified event name (key)
        /// and context properties. You may attach arbitrary data to the event by calling
        /// <see cref="Track(string, Context, LdValue)"/> instead.
        /// </para>
        /// <para>
        /// Note that event delivery is asynchronous, so the event may not actually be sent until
        /// later; see <see cref="LdClient.Flush"/>.
        /// </para>
        /// </remarks>
        /// <param name="name">the name of the event</param>
        /// <param name="context">the evaluation context</param>
        void Track(string name, Context context);

        /// <summary>
        /// Tracks that an application-defined event occurred, and provides an additional numeric value for
        /// custom metrics.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This value is used by the LaunchDarkly experimentation feature in numeric custom metrics,
        /// and will also be returned as part of the custom event for Data Export.
        /// </para>
        /// <para>
        /// Note that event delivery is asynchronous, so the event may not actually be sent until
        /// later; see <see cref="LdClient.Flush"/>.
        /// </para>
        /// </remarks>
        /// <param name="name">the name of the event</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="data">additional data associated with the event; use <see cref="LdValue.Null"/> if
        /// not applicable</param>
        /// <param name="metricValue">a numeric value used by the LaunchDarkly experimentation feature in
        /// numeric custom metrics</param>
        void Track(string name, Context context, LdValue data, double metricValue);

        /// <summary>
        /// Tracks that an application-defined event occurred.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method creates a "custom" analytics event containing the specified event name (key),
        /// context properties, and optional custom data. If you do not need custom data, pass
        /// <see cref="LdValue.Null"/> for the last parameter or simply omit the parameter.
        /// </para>
        /// <para>
        /// Note that event delivery is asynchronous, so the event may not actually be sent until
        /// later; see <see cref="LdClient.Flush"/>.
        /// </para>
        /// </remarks>
        /// <param name="name">the name of the event</param>
        /// <param name="context">the evaluation context</param>
        /// <param name="data">additional data associated with the event, if any</param>
        void Track(string name, Context context, LdValue data);

        /// <summary>
        /// Returns an object that encapsulates the state of all feature flags for a given context, which
        /// can be passed to front-end code.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The object returned by this method contains the flag values as well as other metadata that
        /// is used by the LaunchDarkly JavaScript client, so it can be used for
        /// <see href="https://docs.launchdarkly.com/sdk/features/bootstrapping#javascript">bootstrapping</see>.
        /// </para>
        /// <para>
        /// This method will not send analytics events back to LaunchDarkly.
        /// </para>
        /// </remarks>
        /// <param name="context">the evaluation context</param>
        /// <param name="options">optional <see cref="FlagsStateOption"/> values affecting how the state is
        /// computed  -  for instance, to filter the set of flags to only include the client-side-enabled ones</param>
        /// <returns>a <see cref="FeatureFlagsState"/> object (will never be null; see
        /// <see cref="FeatureFlagsState.Valid"/></returns>
        FeatureFlagsState AllFlagsState(Context context, params FlagsStateOption[] options);

        /// <summary>
        /// Creates a hash string that can be used by the JavaScript SDK to identify a context.
        /// </summary>
        /// <remarks>
        /// See <see href="https://docs.launchdarkly.com/sdk/features/secure-mode#configuring-secure-mode-in-the-javascript-client-side-sdk">Secure mode</see> in
        /// the JavaScript SDK Reference.
        /// </remarks>
        /// <param name="context">the evaluation context</param>
        /// <returns>the hash, or null if the hash could not be calculated</returns>
        string SecureModeHash(Context context);
    }
}