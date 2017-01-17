namespace deleteonerror.MercurialWrapper.Model
{
  /// <summary>
  /// List of possible Change Types
  /// </summary>
  public enum ChangeType
  {
    /// <summary>
    /// a feature
    /// </summary>
    Feature,
    /// <summary>
    /// a specification
    /// </summary>
    Specification,
    /// <summary>
    /// a merge
    /// </summary>
    Merge,
    /// <summary>
    /// a refactoring
    /// </summary>
    Refactoring,
    /// <summary>
    /// a bugfix
    /// </summary>
    Bugfix,
    /// <summary>
    /// a change made by the system
    /// </summary>
    System
  }
}