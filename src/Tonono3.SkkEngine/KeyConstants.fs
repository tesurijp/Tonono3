namespace Tonono3.SKKEngine

[<AbstractClass; Sealed>]
type SkkKeyConstants private () =
    static member VkTab = 0x09
    static member VkReturn = 0x0D
    static member VkEscape = 0x1B
    static member VkSpace = 0x20
    static member VkLeft = 0x25
    static member VkA = 0x41
    static member VkG = 0x47
    static member VkJ = 0x4A
    static member VkK = 0x4B
    static member VkL = 0x4C
    static member VkQ = 0x51
    static member VkSlash = 0xBF

module internal KeyCode =
    [<Literal>]
    let Back = 0x08
    [<Literal>]
    let Tab = 0x09
    [<Literal>]
    let Return = 0x0D
    [<Literal>]
    let Escape = 0x1B
    [<Literal>]
    let Space = 0x20
    [<Literal>]
    let NavigationFirst = 0x21
    [<Literal>]
    let NavigationLast = 0x28
    [<Literal>]
    let A = 0x41
    [<Literal>]
    let D = 0x44
    [<Literal>]
    let F = 0x46
    [<Literal>]
    let G = 0x47
    [<Literal>]
    let J = 0x4A
    [<Literal>]
    let K = 0x4B
    [<Literal>]
    let L = 0x4C
    [<Literal>]
    let N = 0x4E
    [<Literal>]
    let P = 0x50
    [<Literal>]
    let Q = 0x51
    [<Literal>]
    let S = 0x53
    [<Literal>]
    let X = 0x58
    [<Literal>]
    let Slash = 0xBF

module internal DisplayPrefix =
    [<Literal>]
    let Composition = "▽"
    [<Literal>]
    let Conversion = "▼"
