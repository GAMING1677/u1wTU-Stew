# u1wTU-Stew

## 開発開始手順（clone → ブランチ作成 → upstream 設定まで）

この手順では、GitHub 上のリポジトリを **clone** し、  
作業用ブランチを作成して、最初の **push で upstream（追跡ブランチ）を設定**するところまでを説明します。

---

### 1. 前提条件

- Git がインストールされていること
- GitHub アカウントを持っており、push 権限があること

---

### 2. リポジトリを clone する

まず、作業したいディレクトリを作成して、そのディレクトリに移動します。

```bash
mkdir C:\Develop #作業したいフォルダを作成します
cd C:\Develop

#右上緑色のCodeボタンのhttpsというのを選択したら見れる奴です！
git clone https://github.com/GAMING1677/u1wTU-Stew.git
```

cd <REPO>

### 3. origin（リモート）が設定されているか確認する

git remote -v

想定される出力例：

origin https://github.com/<OWNER>/<REPO>.git (fetch)
origin https://github.com/<OWNER>/<REPO>.git (push)

※ git clone を行っていれば、通常は自動で設定されています。

### 4. リモートブランチ情報を取得する

git fetch origin

### 5. 作業用ブランチを作成して切り替える

git switch -c <branch>

例：

git switch -c feature/login

※ git switch が使えない場合は以下でも同じです。

git checkout -b feature/login

### 6. 変更を作成する

例として、README.md を編集するなど、何かしらの変更を加えて保存します。

### 7. 変更をステージしてコミットする

#### 7-1. 状態を確認

git status

#### 7-2. 変更をステージする

git add .

#### 7-3. コミットする

git commit -m "初回コミット"

### 8. 初回 push で upstream を設定する

初回 push では、以下のコマンドを使います。

```
git push --set-upstream origin <branch>
```

例：

```
git push --set-upstream origin feature/login
```

これにより、

ローカルブランチ：`feature/login`

リモートブランチ：`origin/feature/login`

が紐づきます。

[origin/feature/login] が表示されていれば成功です。

### 9. 次回以降の push

upstream 設定後は、次回以降このコマンドだけで push できます。

git push
