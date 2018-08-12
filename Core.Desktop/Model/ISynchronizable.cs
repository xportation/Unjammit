﻿using System.Security.Cryptography.X509Certificates;

namespace Jam.NET.Model
{
  /// <summary>
  /// Something that can be synchronized to the current position by updating some properties.
  /// </summary>
  public interface ISynchronizable
  {
    long SamplePosition { set; }
    long Samples { set; }
  }
}