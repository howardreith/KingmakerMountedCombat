# Chunk 7 request-shaped evidence

`RuntimeLoaderInventory` writes the `JObject` it receives. It does not inherit the
host's camel-case request serializer. The deployed ordinary-acquisition artifact
therefore contains the exact PascalCase `Chunk7OrdinaryAcquisitionSelection`
properties produced by `JObject.FromObject(selection)`.

The read-only field bridge admits that existing source through
`Chunk7EvidenceDto.ReadDeployedOrdinarySelection`. It requires the complete exact
deployed property set, validates the selection, and compares the parsed value to
the actual DTO serialization before returning a camel-case projection. Coercible
strings, floating-point integers, case aliases, extra and missing fields refuse.
The source bytes and archive remain unchanged.

New request-shaped artifact observations call `Chunk7EvidenceDto.Request`
explicitly. This applies to the field bridge selection and its normalized source
body selection. Archive, native body and pose observations retain their separately
declared formats. This helper grants no native restoration or action authority.

The focused regression compiles the real DTOs and helper, serializes actual DTO
instances, and passes those outputs to the strict field reader. The original
default field serialization fails that reader; the explicit projection passes.
The source lookup failure and original field29 map remain preserved in private
evidence. These are offline serialization checks, not native acquisition,
field-placement, Small profile, pose or persistence qualification.
