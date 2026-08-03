# Tonono3 - SKK-like Japanese Input Tool

Tonono3 は、Windows 上で動作する SKK 風の日本語入力ツールです。
概ね動くようになった[Tonono2](https://github.com/tesurijp/Tonono2) を最近慣れてきたF#へ、半分AI、半分手動で置き換えを行なったものです。  
機能や操作方法などは Tonono2と同じ。内部の実装のみが異なります。  
合わせて[tsr-di](https://github.com/tesurijp/tsr-di) を利用したコードになっています(後付けで利用したので、開発時のメリットはあまり無かったですが、、)  

## 主な機能

- SKK 風の変換開始、送り仮名付き変換、候補送り
- ひらがな、カタカナ、全角英数、無効化状態の切り替え
- 辞書補完
- 未登録語のその場登録とユーザー辞書への保存
- ユーザー辞書からの候補削除
- `config.yaml` の自動再読み込み
- タスクトレイ常駐、設定表示、再起動、終了

## 動作環境

- Windows
- .NET 10

## 起動方法

`Tonono3.exe` を実行すると常駐します。
多重起動は制限しています。

起動後の挙動:

- タスクトレイにアイコンが表示されます
- 入力バッファがある間だけ、キャレット近くに状態表示ウィンドウが出ます
- キャレット位置が取れないアプリでは、マウスカーソル付近に表示されます
- タスクトレイメニューから以下の操作が可能です。
  - 情報  設定値の一覧表示画面を開きます。
  - 設定 現在の設定ファイルを関連付けられたエディタで開きます。
  - 再起動 Tonono3 を再起動します。(プロセスを終了し開きなおします)
  - 終了 Tonono3を終了します。

## キー操作

- キー操作のカスタマイズは想定していません。
実装されている主な操作は次のとおりです。

| キー | 動作 |
|:---|:---|
| `Ctrl + J` | かな入力の開始、入力を確定 |
| `q` | ひらがな/カタカナ切り替え、入力中なら反転確定 |
| `l` | 無効化して直接入力モードへ移行 |
| `Shift + l` | 全角英数モードへ移行 |
| `/` | abbrev モード開始 |
| `Shift + 英字` | 変換開始 / 送り仮名付き変換の開始 |
| `Tab` | 辞書補完 / 次の補完候補 |
| `Space` | 変換開始 / 次候補 / 補完候補の採用 |
| `Enter` | 現在の内容を確定 |
| `Ctrl + G` | 入力・変換・語句登録のキャンセル |
| `Esc` | 入力・変換のキャンセル |
| `Backspace` | バッファの1文字削除 |
| `Ctrl + N` | 次候補 |
| `Ctrl + P` | 前候補 |
| `Ctrl + X` | 現在候補をユーザー辞書から削除 |
| `A` `S` `D` `F` `J` `K` `L` | 変換候補一覧表示中の候補を直接選択 |

補足:

- `Esc` は、設定した vi 互換アプリ上では Tonono3 側を無効化したうえでアプリへ `Esc`を そのまま渡します
- 候補が尽きると単語登録モードへ入ります
- 送り仮名付き変換では、確定時に送り仮名部分も一緒に出力されます

## 設定ファイル

設定ファイル名は `config.yaml` です。

通常ビルドでは次の優先順位で読み込まれます。

1. `%AppData%\Tonono3\config.yaml`
2. 実行ファイルと同じフォルダの `config.yaml`

`Debug` ビルドでは、開発環境上でのデバッグを想定しており、実行ファイルと同じフォルダの `config.yaml` に固定されます。

設定ファイルは `FileSystemWatcher` で監視します。再読み込みが完了すると、辞書と変換テーブルは次のキー入力処理から一括して反映されます。

### 設定項目

- `dictionaryPaths`
  使用する SKK 辞書の配列です。相対パスは `config.yaml` からの相対として解決されます。`.gz` も読めます。
- `userDictionaryPath`
  ユーザー辞書の保存先です。単語登録や候補削除はこの辞書に対して行われます。
- `viCompatibleApps`
  `Esc` を特別扱いするアプリの実行ファイル名です。アクティブプロセスのフルパス末尾と比較します。
- `romajiTable`
  ローマ字かな変換表です。
- `romajiTable.moraModifier`
  促音や撥音に変換するための入力パターンです。設定のパターンを仮名に置き換え、読み候補末尾の1文字を残します。
- `romajiTable.moraAutoComplete`
  変換開始時など、末尾に未確定の読み候補があった場合に補完します。既定では `n -> ん` だけです。
- `zenkakuTable`
  全角英数モードでの変換表です。

同梱の [src/Tonono3/resources/config.yaml](src/Tonono3/resources/config.yaml) がサンプルです。

## 辞書

既定設定では次の辞書を利用します。

- `SKK-JISYO.L.gz`
- `SKK-JISYO.jinmei.gz`
- `user-jisyo.txt`

`Debug` ビルドでは、`resources` 配下に辞書がなければビルド時に `https://skk-dev.github.io/dict/` から `SKK-JISYO.L.gz` と `SKK-JISYO.jinmei.gz` を取得します。

辞書読み込み時は UTF-8 を優先し、判定できない場合は EUC-JP として扱います。

## ビルド

通常ビルド:

```powershell
dotnet build
```

ネイティブ AOT publish:

```powershell
dotnet publish src/Tonono3/Tonono3.csproj -c NativeAot
```

Native AOT の出力先はソリューションフォルダ直下の `publish\` です。このフォルダは `dotnet clean` で削除されます。

## 実装メモ

- C#からF#への移行で、修正量が多そうなのでテストコードを追加しました。
- キーボードフックは `src/Tonono3/Win32/Keyboard.cs` と `SkkController` で管理しています
- 状態遷移は `src/Tonono3.SkkEngine` の F# reducer に実装し、状態操作・合成処理・入力分岐・表示生成でファイルを分離しています
- C# UI／Controller から使用するキー定数も F# の `KeyConstants.fs` を参照します
- システム辞書は読み込み完了後、C#／F# ともに不変コレクションとして保持します
- UI は Avalonia のウィンドウ 1 枚とタスクトレイアイコンで構成されています
