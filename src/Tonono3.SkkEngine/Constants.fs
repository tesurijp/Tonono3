namespace Tonono3.SkkEngine

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
    let G = 0x47
    [<Literal>]
    let J = 0x4A
    [<Literal>]
    let L = 0x4C
    [<Literal>]
    let N = 0x4E
    [<Literal>]
    let P = 0x50
    [<Literal>]
    let Q = 0x51
    [<Literal>]
    let X = 0x58
    [<Literal>]
    let Slash = 0xBF

module internal ConfigDefaults =
    [<Literal>]
    let CandidateSelectionKeys = "ASDFJKL"

module internal DisplayPrefix =
    [<Literal>]
    let Composition = "▽"
    [<Literal>]
    let Conversion = "▼"
