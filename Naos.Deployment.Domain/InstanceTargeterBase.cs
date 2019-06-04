// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InstanceTargeterBase.cs" company="Naos Project">
//    Copyright (c) Naos Project 2019. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Naos.Deployment.Domain
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    using static System.FormattableString;

    /// <summary>
    /// Model class to contain information to find an instance.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
    [Bindable(BindableSupport.Default)]
    public class InstanceTargeterBase
    {
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a specific ID from the computing platform provider.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
    public class InstanceTargeterSystemId : InstanceTargeterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTargeterSystemId"/> class.
        /// </summary>
        /// <param name="instanceId">ID of instance to target.</param>
        public InstanceTargeterSystemId(string instanceId)
        {
            this.InstanceId = instanceId;
        }

        /// <summary>
        /// Gets the ID (per the computing platform provider) of the instance to change the type of.
        /// </summary>
        public string InstanceId { get; private set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Invariant($"{this.GetType()} - {nameof(this.InstanceId)}: {this.InstanceId}");
        }
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a name to be looked.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
    public class InstanceTargeterNameLookup : InstanceTargeterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTargeterNameLookup"/> class.
        /// </summary>
        /// <param name="name">Name to lookup in the arcology to find the instance to target (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a).</param>
        public InstanceTargeterNameLookup(string name)
        {
            this.Name = name;
        }

        /// <summary>
        /// Gets the name of the instance (short name - i.e. 'Database' NOT 'instance-Development-Database@us-west-1a)'.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Invariant($"{this.GetType()} - {nameof(this.Name)}: {this.Name}");
        }
    }

    /// <summary>
    /// Implementation of InstanceTargeterBase that uses a tag or tags to match.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Targeter", Justification = "Spelling/name is correct.")]
    public class InstanceTargeterTagMatch : InstanceTargeterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceTargeterTagMatch"/> class.
        /// </summary>
        /// <param name="tags">Tags to match in the arcology.</param>
        /// <param name="tagMatchStrategy">Optional strategy to use for matching tags; DEFAULT is all.</param>
        public InstanceTargeterTagMatch(IReadOnlyDictionary<string, string> tags, TagMatchStrategy tagMatchStrategy = TagMatchStrategy.All)
        {
            this.TagMatchStrategy = tagMatchStrategy;
            this.Tags = tags ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the tags to match'.
        /// </summary>
        public IReadOnlyDictionary<string, string> Tags { get; private set; }

        /// <summary>
        /// Gets the tag match strategy.
        /// </summary>
        public TagMatchStrategy TagMatchStrategy { get; private set; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Invariant($"{this.GetType()} - {nameof(this.TagMatchStrategy)}: {this.TagMatchStrategy}; {nameof(this.Tags)}: {string.Join(",", this.Tags.Select(_ => _.Key + "=" + _.Value))}");
        }
    }
}
