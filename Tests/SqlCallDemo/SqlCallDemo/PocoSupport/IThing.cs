using CK.Core;
using System;

namespace SqlCallDemo;

public interface IThing : IPoco
{
    string Name { get; set; }

    Guid UniqueId { get; set; }
}
