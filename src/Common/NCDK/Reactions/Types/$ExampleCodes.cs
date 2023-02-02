
namespace NCDK.Reactions.Types
{
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new AdductionProtonLPReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class AdductionProtonLPReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new AdductionProtonPBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class AdductionProtonPBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new AdductionSodiumLPReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class AdductionSodiumLPReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new CarbonylEliminationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class CarbonylEliminationReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new ElectronImpactNBEReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class ElectronImpactNBEReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new ElectronImpactPDBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class ElectronImpactPDBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new ElectronImpactSDBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class ElectronImpactSDBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new HeterolyticCleavagePBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class HeterolyticCleavagePBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new HeterolyticCleavageSBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class HeterolyticCleavageSBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new HomolyticCleavageReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class HomolyticCleavageReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new HyperconjugationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class HyperconjugationReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new PiBondingMovementReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class PiBondingMovementReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalChargeSiteInitiationHReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalChargeSiteInitiationHReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalChargeSiteInitiationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalChargeSiteInitiationReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteHrAlphaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteHrAlphaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteHrBetaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteHrBetaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteHrDeltaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteHrDeltaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteHrGammaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteHrGammaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteInitiationHReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteInitiationHReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteInitiationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteInitiationReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteRrAlphaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteRrAlphaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteRrBetaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteRrBetaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteRrDeltaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteRrDeltaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RadicalSiteRrGammaReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RadicalSiteRrGammaReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RearrangementAnionReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RearrangementAnionReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RearrangementCationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RearrangementCationReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RearrangementLonePairReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RearrangementLonePairReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new RearrangementRadicalReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class RearrangementRadicalReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new SharingAnionReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class SharingAnionReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new SharingChargeDBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class SharingChargeDBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new SharingChargeSBReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class SharingChargeSBReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new SharingLonePairReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class SharingLonePairReaction {}
    /// <example>
    /// <code>
    /// var setOfReactants = ChemObjectBuilder.Instance.NewAtomContainerSet&lt;IAtomContainer&gt;();
    /// setOfReactants.Add(molecular);
    /// var type = new TautomerizationReaction();
    /// var param = new SetReactionCenter();
    /// param.IsSetParameter = false;
    /// var paramList = new[] { param };
    /// type.ParameterList = paramList;
    /// IReactionSet setOfReactions = type.Initiate(setOfReactants, null);
    ///  </code>
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <code>atoms[0].IsReactiveCenter = true;</code>
    /// <para>Moreover you must put the parameter true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </example>
    public partial class TautomerizationReaction {}
}
