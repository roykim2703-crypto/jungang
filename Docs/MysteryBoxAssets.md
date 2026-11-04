# 개꿀잼 상자 에셋

내장 `image_gen` 도구로 생성한 투명 PNG 두 장을 각각 사용합니다. 원본 픽셀과 알파를 보존하고 Unity에서 Sprite / Single, Point 필터, 무압축으로 임포트했습니다. 몸통과 뚜껑은 독립적인 UI Image로 움직입니다.

- 몸통: `Assets/Resources/Effects/MysteryBoxBody.png`
- 뚜껑: `Assets/Resources/Effects/MysteryBoxLid.png`
- 연출: `Assets/김민균/Script/Card/MysteryBoxView.cs`

## 몸통 생성 프롬프트

Create ONE production game sprite: ONLY THE BODY/base of a black mystery loot box, WITHOUT ITS LID. Transparent background, isolated centered object, square canvas, no ground or cast shadow. A stout broad rectangular black lacquered wooden chest base with charcoal navy faces, beveled thick edges, subtle antique gold rim and small central gold diamond clasp. Front-facing orthographic with very slight top view: the top opening is a broad shallow trapezoid with dark interior; left/right symmetrical, no perspective rotation. The open body silhouette about 760px wide and 460px high on a 1024 square transparent canvas. Chunky polished 2D pixel-art illustration matching a retro Korean board game: deliberate pixel-stepped outlines, carefully shaded broad surfaces, readable at 300px. LID ABSOLUTELY ABSENT: no lid attached, floating, behind, or covering opening. No stones, no props, no text, no labels, no particles, no background. This will be animated with a separately generated matching detachable lid.

## 뚜껑 생성 프롬프트

몸통 이미지를 재질·화풍·시점의 참고 이미지로 제공했습니다.

Generate ONLY a DETACHED MATCHING LID for the black mystery loot chest BODY in the reference image. The reference is for material, pixel-art shading, front-facing symmetric slight-top-view perspective and gold-trim geometry; do not reproduce its body. One isolated low flat rectangular lid, broad shallow trapezoid top surface, near edge wider than far edge, charcoal-black lacquer wood with antique-gold beveled borders, small subtle diamond motif centered on the top. A short black front lip. This lid must sit over the opening of that exact body and can be lifted as its own Unity UI sprite. Match reference camera angle and width, left-right symmetric. Thick deliberate pixel-step outlines, premium polished retro 2D board game style, dark navy-black material and warm gold. Fully visible centered with generous transparent margin all around. ABSOLUTELY NO CHEST BODY, no container/base, no hinges sticking off, no floating effects, no letters, no ground, no cast shadow, no background. The single detached lid should span approximately 82 percent of canvas width and about 30 percent of square-canvas height. Genuine transparent alpha background.
