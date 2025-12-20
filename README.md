# u1wTU-Stew
開発開始手順（clone → ブランチ作成 → upstream 設定まで）

この手順では、GitHub 上のリポジトリを clone し、
作業用ブランチを作成して、最初の push で upstream（追跡ブランチ）を設定するところまでを説明します。

## 1. 前提条件

*   Git がインストールされていること
*   GitHub アカウントを持っており、push 権限があること

## 2. リポジトリを clone する

まず、作業したいディレクトリを作成して、そのディレクトリに移動します。

```bash
mkdir C:\Develop #作業したいフォルダを作成します
cd C:\Develop
```

#右上緑色のCodeボタンのhttpsというのを選択したら見れる奴です！

```bash
git clone https://github.com/GAMING1677/u1wTU-Stew.git
cd u1wTU-Stew
```

## 3. origin（リモート）が設定されているか確認する

```bash
git remote -v
```

想定される出力例：

```
origin https://github.com/GAMING1677/u1wTU-Stew.git (fetch)
origin https://github.com/GAMING1677/u1wTU-Stew.git (push)
```

※ git clone を行っていれば、通常は自動で設定されています。

## 4. リモートブランチ情報を取得する

```bash
git fetch origin
```

## 5. 作業用ブランチを作成して切り替える

```bash
git switch -c <ブランチ名>
```

例：

```bash
git switch -c feature/login
```

※ git switch が使えない場合は以下でも同じです。

```bash
git checkout -b feature/login
```

## 6. 変更を作成する

例として、README.md を編集するなど、何かしらの変更を加えて保存します。

## 7. 変更をステージしてコミットする

### 7-1. 状態を確認

```bash
git status
```

### 7-2. 変更をステージする

```bash
git add .
```

### 7-3. コミットする

```bash
git commit -m "初回コミット"
```

## 8. 初回 push で upstream を設定する

初回 push では、以下のコマンドを使います。

```bash
git push --set-upstream origin <branch>
```

例：

```bash
git push --set-upstream origin feature/login
```

これにより、

ローカルブランチ：feature/login

リモートブランチ：origin/feature/login

が紐づきます。

`[origin/feature/login]` が表示されていれば成功です。

## 9. 次回以降の push

upstream 設定後は、次回以降このコマンドだけで push できます。

```bash
git push
```
