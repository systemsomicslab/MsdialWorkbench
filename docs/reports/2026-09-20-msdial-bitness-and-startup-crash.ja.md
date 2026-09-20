# MS-DIAL ファミリーの 32bit / 64bit 実態調査と MsdialGuiApp の x64 明示化

対象リポジトリ: `MsdialWorkbench`
調査日: 2026-09-20
調査対象ブランチ: `feature/annotation-evidence-record`
きっかけ: ユーザーから「エクスポートすると突然終了する」「起動時に例外ダイアログが出る」という
二重のバグ報告があり、その原因調査の過程でプロセスのビット数を確認したもの。

---

## 0. 結論から

**「Prefer32Bit が true になっていた箇所」は、MS-DIAL 本体には存在しませんでした。**
MS-DIAL 5 GUI（`MSDIAL.exe`）は以前から 64bit プロセスとして動作しています。
明示的に `<Prefer32Bit>true</Prefer32Bit>` と書かれていたのは **SpectrumViewer の 2 構成だけ**です。

その上で、MsdialGuiApp については「64bit であること」を暗黙の既定値に頼らず
プロジェクトファイルに明記する変更を入れました（第 4 節）。これは不具合修正ではなく、
意図の明文化と将来の退行防止です。

---

## 1. 調査で最初に誤った点（記録として残します）

最初、`MSDIAL.exe` の PE ヘッダを読んで次の値を確認し、32bit プロセスだと判断しました。

```
machine        = 0x014C (IMAGE_FILE_MACHINE_I386)
optional magic = 0x010B (PE32)
characteristics= LARGE_ADDRESS_AWARE
```

**この判断は誤りです。** マネージド実行ファイルの場合、AnyCPU でビルドされたアセンブリも
PE ヘッダ上は I386 / PE32 になります。32bit として起動されるかどうかを決めるのは PE ヘッダではなく、
CLI ヘッダ（データディレクトリ 14 番）の `COMIMAGE_FLAGS` にある次の 2 ビットです。

| フラグ | 値 | 意味 |
| --- | --- | --- |
| `COMIMAGE_FLAGS_32BITREQUIRED` | 0x00000002 | 32bit プロセスでしか起動しない |
| `COMIMAGE_FLAGS_32BITPREFERRED` | 0x00020000 | 64bit OS でも 32bit を選ぶ（AnyCPU 32-bit preferred） |

変更前のビルド成果物で実測した値は以下のとおりです。

```
src\MSDIAL5\MsdialGuiApp\bin\Debug\net48\MSDIAL.exe   (2026-09-16 ビルド)
  CorFlags        = 0x1
  ILONLY          = True
  32BITREQUIRED   = False
  32BITPREFERRED  = False
  → 純粋な AnyCPU。64bit Windows 上では 64bit プロセスとして動作する。
```

すなわち MS-DIAL 5 GUI は従来から 64bit で動いており、
「32bit の 4GB 上限にエクスポートが当たっている」という当初の仮説は成立しません。

---

## 2. 既定値が SDK スタイルと従来スタイルで逆であること

誤判断の原因はここにあります。`Prefer32Bit` の既定値は、プロジェクトファイルの形式によって逆になります。

| プロジェクト形式 | 既定値 | 定義場所 |
| --- | --- | --- |
| 従来形式（非 SDK スタイル） | **true**（exe かつ .NET Framework 4.5 以降） | `Microsoft.Common.CurrentVersion.targets` |
| SDK スタイル（`<Project Sdk="Microsoft.NET.Sdk">`） | **false** | `Microsoft.NET.Sdk.props:115` <br> `<Prefer32Bit Condition="'$(Prefer32Bit)'==''">false</Prefer32Bit>` |

本リポジトリの実行ファイルプロジェクトは、確認した範囲ではすべて SDK スタイルに移行済みでした。

```
SDK-style    src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj
SDK-style    src\MSDIAL5\SpectrumViewer\SpectrumViewer.csproj
SDK-style    src\MSFINDER\MSFINDER\MSFINDER.csproj
SDK-style    src\RawDataApp\RawDataConverter\RawDataConverter.csproj
SDK-style    src\RawDataApp\RawDataViewer\RawDataViewer.csproj
SDK-style    src\MSDIAL4\MsDial\MSDIAL.csproj
SDK-style    tests\MSDIAL5\MsdialCoreTestApp\MsdialCoreTestApp.csproj
```

したがって「AnyCPU かつ `Prefer32Bit` 無指定」は、このリポジトリでは 64bit を意味します。
ただしこれは SDK スタイルに移行したことによる結果であり、明記されているわけではありません。
将来いずれかのプロジェクトが従来形式に戻る、あるいは新規プロジェクトが従来形式で追加されると、
既定値が反転して黙って 32bit になります。第 4 節の変更はこの穴を塞ぐことが目的です。

---

## 3. 全実行ファイルのビット数実測結果

リポジトリ内の `bin\` 配下にある実行ファイルすべてについて CLI ヘッダのフラグを読み、
実際にどちらで起動するかを判定しました。

### 64bit で動作（AnyCPU、問題なし）

`MSDIAL.exe`（MS-DIAL 5 GUI / MS-DIAL 4 GUI）、`MSDIALCUI.exe`（MS-DIAL 5 コンソール）、
`MSFINDER.exe`、`MsfinderConsoleApp.exe`、`MsdialConsoleApp.exe`、
`RawDataViewer.exe`、`RawDataConverter.exe`、`LipidomicsRtManager.exe`、
`PathwayMapApp.exe`、`ColorPickerWpfApp.exe`、各種 ConsoleApp 類。

### 32bit で動作している成果物（AnyCPU 32-bit preferred）

| 実行ファイル | 該当ビルド数 | 備考 |
| --- | --- | --- |
| `SpectrumViewer.exe` | 1 | **csproj に `<Prefer32Bit>true</Prefer32Bit>` の明示あり**（下記） |
| `MolViewer.exe` | 5 | MSDIAL4 系。同名で 64bit の成果物も 7 個あり、古いビルドが残っているもの |
| `MonaRunner.exe` | 1 | 同上 |
| `MsDialMassqlTestApp.exe` | 1 | 同上 |

`SpectrumViewer` 以外は、SDK スタイル移行前にビルドされた古い成果物が `bin\` に残っているだけと
考えられます（同じ exe 名で 64bit の成果物が併存しているため）。クリーンビルドすれば解消する想定です。

### 明示的に `Prefer32Bit` が指定されている箇所（全リポジトリ）

```
src\MSDIAL5\SpectrumViewer\SpectrumViewer.csproj
  46:  <Prefer32Bit>true</Prefer32Bit>    ← 'Debug vendor unsupported|AnyCPU' 構成
  56:  <Prefer32Bit>true</Prefer32Bit>    ← 'Release vendor unsupported|AnyCPU' 構成

src\MSFINDER\MSFINDER\MSFINDER.csproj
  28:  <Prefer32Bit>false</Prefer32Bit>   ← 'Debug|AnyCPU' 構成
  38:  <Prefer32Bit>false</Prefer32Bit>   ← 'Release|AnyCPU' 構成
  （'Debug vendor unsupported' と 'Release vendor unsupported' の 2 構成には指定がない）
```

**確認をお願いしたい点**：SpectrumViewer の `vendor unsupported` 構成だけが 32bit 指定になっているのは
意図的でしょうか。もし意図がないのであれば `false` に揃えるのが自然です。
MSFINDER 側も、通常構成には `false` があるのに `vendor unsupported` 構成にはないという非対称があり、
どちらも「構成を追加したときに書き漏れた」ように見えます。

---

## 4. 実施した変更（`src\MSDIAL5\MsdialGuiApp\MsdialGuiApp.csproj`）

```diff
-		<PlatformTarget>AnyCPU</PlatformTarget>
+		<PlatformTarget>x64</PlatformTarget>
+		<Prefer32Bit>false</Prefer32Bit>
+		<UseCurrentRuntimeIdentifier>false</UseCurrentRuntimeIdentifier>
```

加えて、`System.Configuration` の明示参照を 1 行追加しています（第 5 節の修正で
`ConfigurationErrorsException` を使うため。既定の暗黙参照には含まれていませんでした）。

### なぜ AnyCPU のままにしなかったか

同梱しているベンダーリーダーのうち以下は **x64 専用**です。
32bit プロセスでは `BadImageFormatException` でロードそのものができません。

| ファイル | 種別 | アーキテクチャ |
| --- | --- | --- |
| `lib\Agilent\BaseTof.dll` | マネージド | AMD64（x64 専用としてコンパイル） |
| `lib\Waters\MassLynxSDK.dll` | マネージド | AMD64（同上） |
| `lib\Bruker\timsdata.dll` | ネイティブ | AMD64 |
| `lib\Bruker\baf2sql_c.dll` | ネイティブ | AMD64 |

つまり MS-DIAL にとって 64bit は「望ましい」ではなく「必須」です。
それを SDK の既定値に依存して暗黙に満たすのではなく、プロジェクトファイルに書くことにしました。

なお Shimadzu だけは `lib\Shimadzu\x86` と `lib\Shimadzu\x64` の両方を同梱しており、
どちらのビット数でも動く構成になっています。x64 固定にしても `x64` 側が使われるだけで問題ありません。

### `UseCurrentRuntimeIdentifier=false` を足した理由

`PlatformTarget` に具体的なアーキテクチャを書くと、.NET SDK 10 はランタイム識別子
（`win-x64`）付きの復元を要求し、.NET Framework プロジェクトではアセットが無いため
`NETSDK1047` でビルドが失敗します。自己完結型の発行をしているわけではなく、
生成されるバイナリの読み込まれ方を指定したいだけなので、識別子の推論を断っています。

### 検証結果

- `net48` / `net481` / `net472` の 3 ターゲットすべてでビルド成功（追加オプションなし）。
- 生成された `MSDIAL.exe` は `machine=0x8664`, `PE32+`。
- 実際に起動して確認：メインウィンドウが表示され、`IsWow64Process = False`（真正の 64bit プロセス）。

### 合わせて確認をお願いしたい点

1. **数値再現性**。今回の変更で「AnyCPU（実行時 64bit）」から「x64 固定」に変わりましたが、
   JIT が生成するコードは従来も 64bit だったため、計算結果は変わらない見込みです。
   ただし念のため、リリース前にリファレンスデータで既存出力と一致することの確認を推奨します。
2. `<HighEntropyVA>false</HighEntropyVA>` が MsdialGuiApp.csproj に明示されています。
   64bit の高エントロピー ASLR を無効化する設定で、32bit 時代の名残か、
   特定のベンダー DLL のために意図的に置かれたのか判断できませんでした。今回は触れていません。
3. **ソリューションの構成写像**。`MsdialWorkbench.sln` には `Release|x64` などの
   x64 プラットフォームが定義されていますが、`MsdialGuiApp` の写像は
   `Release|x64.ActiveCfg = Release|Any CPU` となっており、x64 を選んでも Any CPU がビルドされます。
   今回の `PlatformTarget` 指定によって結果としては x64 が出力されるため実害はありませんが、
   紛らわしいので整理を検討いただければと思います。
4. **コンソール版（`MSDIALCUI.exe`）は変更していません。** 現状 AnyCPU で 64bit 動作しており、
   公開リポジトリ再解析パイプラインが出力のビット単位での再現性に依存しているため、
   同じ変更を入れるかどうかは別途ご判断ください。

---

## 5. 同時に修正したバグ報告関連の 2 件

今回の x64 明示化は、冒頭のバグ報告そのものの修正ではありません。実際の修正は以下です。

### 5-1. 起動時例外の原因が報告書から読み取れない問題

`src\MSDIAL5\MsdialGuiApp\App.xaml.cs` の `LogUnhandledException` は
`exception.Message` と `exception.StackTrace` だけを表示していました。
`MainWindow` のコンストラクタ内で何かが例外を投げると、WPF は必ず
「The invocation of the constructor on type ... threw an exception」という同一のメッセージと
WPF 内部だけのスタックトレースを持つ `XamlParseException` を返すため、
**原因は `InnerException` 側にしか存在せず、それを捨てていました。**
ログファイルの出力も一切ありませんでした。

修正内容：

- `InnerException` を末端まで辿って全段を表示。最内の例外をダイアログ冒頭に出す。
- `ConfigurationErrorsException` の場合は設定ファイル名と行番号を、
  `FileNotFoundException` の場合は不足しているアセンブリ名と Fusion ログを追記。
- 同じ内容を
  `%LOCALAPPDATA%\MSDIAL\crash\MSDIAL-crash-<日時>.log` に書き出し、
  そのパスをダイアログに表示（スクリーンショットはスクロールもコピーもできないため）。
- ログにはバージョン、プロセスのビット数、OS、CLR、ワーキングセット、exe のパスを含めた。

### 5-2. 破損した `user.config` で二度と起動できなくなる問題

`src\MSDIAL5\MsdialGuiApp\Model\Core\MainWindowModel.cs` のコンストラクタが
`Properties.Settings.Default` を無防備に読んでいました。ツリー全体で
`ConfigurationErrorsException` を捕捉している箇所は 0 件でした。

設定ファイルは
`%LOCALAPPDATA%\CompMs\MSDIAL.exe_Url_<exe パスのハッシュ>\1.0.0.0\user.config`
に置かれます。`Settings.Save()` の書き込み中にプロセスが死ぬと途中で切れたファイルが残り、
以後の起動は毎回このコンストラクタで落ちます。
しかも `AssemblyVersion` が `1.0.0.0` に固定されているため、**バージョンを上げても
再インストールしても同じファイルを読みに行き、復旧しません。**

修正内容：読めない `user.config` は `user.config.broken-<日時>` に退避し、
既定値で起動を続行。退避先を明示した警告ダイアログを 1 回だけ表示。
後続の `Settings.Save()` も、保存失敗がプロジェクト保存の失敗にならないよう保護。

動作確認：実際に `user.config` を途中で切って起動し、
「MS-DIAL settings were reset」ダイアログののちメインウィンドウが正常に開くこと、
`user.config.broken-20260920220423` が残ることを確認済み。

### 5-3. エクスポートの失敗が握りつぶされる問題

`AlignmentResultExportModel.ExportAlignmentResultAsync` は `Task.Run` の戻り値を
誰も待たないため、例外は Task に捕まったまま誰にも見られませんでした。さらに
`task.End()` が例外経路で呼ばれず、進捗表示が残り続け、
以後アプリを閉じようとするたびに「A process is running in the background」警告が出る状態でした。

修正内容：例外を捕捉して `ErrorMessageBoxRequest` で表示、`task.End()` を `finally` に移動。
`OutOfMemoryException` / `UnauthorizedAccessException` / `IOException` には個別の説明文を用意
（`Model\Export\ExportFailure.cs` を新規追加）。`AnalysisResultExportModel.Export` も同様に保護。

### エクスポート時の突然終了について、残っている疑い

32bit 上限説が消えたため、現時点で有力な仮説は以下です。いずれも crash ログがあれば切り分けできます。

- **単一オブジェクト 2GB 制限。** `App.config` の `<runtime>` に
  `<gcAllowVeryLargeObjects enabled="true" />` が **ありません**。
  .NET Framework では、64bit プロセスであっても単一の配列・文字列が 2GB を超えると
  物理メモリに余裕があっても `OutOfMemoryException` になります。
  大規模アライメント結果のエクスポートで到達しうる範囲です。
  設定を入れるべきかどうかはご判断ください（今回は投機的な変更を避け、未実施です）。
- 実際のメモリ枯渇（搭載メモリの少ない端末）。
- ネイティブベンダー DLL 側のクラッシュ（この場合 .NET の例外ハンドラを経由せず即死します）。
