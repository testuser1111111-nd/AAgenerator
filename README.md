# AAgenerator

高速にアスキーアート自動生成をする

ConsoleApp1のほうが動画再生する時用
WinformsApp1のほうが1枚の画像だけ変換したい時用

受験終わってから手を付ける

動画の再生方法は暇なときに書く

似たようなプロジェクトと比べて何が良いか

* 先駆者は大体ピクセルの明度だけで使う文字を決めており、輪郭が壊れているが、このプログラムは輪郭で使う文字を決めており、少ない文字数でもハッキリと表示できる
* 前もって動画を画像に変換しておけば、リアルタイムで処理するので実質的な前処理の時間が短くて済む
* しかもCPUの性能が良ければ1920x1080pxの動画でも平均100fps出る(検証スペック CPU:Intel Core i7-11800H Memory:DDR4-3200 16GB Storage:SSD　コンパイル時最適化on)
* PentiumG4425Y(Surface Go 2とかに載っている)とかのカスCPUでも20fps程度出る
* 今のところ半角文字しか対応していないが、自由に使用する文字を選べる　フォント変更にも対応可能
* 外部ライブラリはWMPLibとかSystem.DrawingとかのMicrosoft謹製のものしか使ってないのでライセンス的に楽(ただしWindows上でしか今の所動かせない)
