namespace MercurialWrapper.Model
{
  /// <summary>
  /// A user of Mercurial
  /// </summary>
  public class User
  {
    /// <summary>
    /// Gets or sets the name.
    /// </summary>
    /// <value>
    /// The name.
    /// </value>
    public string Name { get; set; }
    /// <summary>
    /// Gets or sets the mail address.
    /// </summary>
    /// <value>
    /// The mail address.
    /// </value>
    public string MailAddress { get; set; }
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="mail">The mail.</param>
    public User(string name, string mail)
    {
      Name = name;
      MailAddress = mail;
    }
  }
}