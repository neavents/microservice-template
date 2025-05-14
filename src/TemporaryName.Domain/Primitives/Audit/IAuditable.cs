using System;

namespace TemporaryName.Domain.Primitives.Audit;

public interface IAuditable : IModifiedAuditable, ICreatedAuditable
{

}
