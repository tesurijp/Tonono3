namespace Tonono3.SKKEngine

type InputMode = Disabled = 0 | Hiragana = 1 | Katakana = 2 | Zenkaku = 3

[<Sealed>]
type KeyCommand(vkCode: int, shift: bool, control: bool, ch: char) =
    member _.VkCode = vkCode
    member _.Shift = shift
    member _.Control = control
    member _.Character = ch

[<Sealed>]
type RegistrationFrame(reading: string, previousMode: InputMode, word: string) =
    member _.Reading = reading
    member _.PreviousMode = previousMode
    member _.Word = word
