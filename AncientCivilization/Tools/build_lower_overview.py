"""Draw a source-data overview of the lower district routes as a standalone SVG."""
import argparse
import hashlib
import html
import json
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('input', type=Path, help='JSON from LowerRouteExport')
    parser.add_argument('--output', type=Path, default=Path('LowerDistrictOverview.svg'))
    args = parser.parse_args()
    source = args.input.read_bytes()
    data = json.loads(source)
    if len(data['cases']) != 8:
        parser.error('The diagram requires all eight lower cases.')
    width, height = 1480, 1320
    parts = [f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" role="img" aria-labelledby="title description">',
             '<title id="title">PLUGIN 하층 생산구와 생활부의 연속 수사로</title>',
             '<desc id="description">실제 코드에서 추출한 여덟 이정표, 필수 현장 48곳과 선택 현장 24곳. Unity 실행 화면과 실측 완주 시간이 아닌 구조도.</desc>',
             f'<!-- Source JSON SHA-256: {hashlib.sha256(source).hexdigest()} -->',
             '<rect width="1480" height="1320" fill="#08131e"/>',
             '<style>text{font-family:"Noto Sans CJK KR","Malgun Gothic",sans-serif;fill:#dbe9ee}.muted{fill:#8ca9ba}.title{font-size:30px;font-weight:700}.head{font-size:22px;font-weight:700}.small{font-size:13px}.body{font-size:16px}</style>']

    def text(x, y, words, css='body', extra=''):
        parts.append(f'<text x="{x}" y="{y}" class="{css}" {extra}>{html.escape(str(words))}</text>')

    def box(x, y, w, h, fill, stroke='#284151', radius=7):
        parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" rx="{radius}" fill="{fill}" stroke="{stroke}"/>')

    def wrap_title(words):
        lines, line, units = [], '', 0
        for char in words:
            size = 1 if ord(char) > 127 else .55
            if units + size > 9.3:
                lines.append(line.rstrip())
                line, units = '', 0
            line += char
            units += size
        if line:
            lines.append(line.rstrip())
        return lines

    text(40, 53, 'PLUGIN / 두 구역의 연속 수사로', 'title')
    text(40, 85, 'v0.25.0 · 필수 현장 48곳 + 선택 현장 24곳 · 새 현장 화자 48명', 'body muted')
    text(40, 112, '첫 플레이 목표 150분 · 실제 완주 시간은 미측정 · 이 그림은 코드 기반 구조도입니다.', 'body muted')
    text(40, 163, '하층 생산구 / 최하층', 'head')
    text(775, 163, '하부 생활구 / 그 위층', 'head')
    text(40, 190, '재고 보고 → 코어 트럭 추격·검거 → 아래 수사로', 'small muted')
    text(775, 190, '실제 엘리베이터 → 도둑이 지나갔다는 길 → 본거지', 'small muted')
    actions = {'Talk': '증언', 'Blade': '베기', 'Relay': '레이저', 'Ambush': '경비전', 'Wire': '와이어', 'Throw': '투척'}
    for case in data['cases']:
        x = 40 + case['floor'] * 735
        y = 212 + case['id'] % 4 * 239
        box(x, y, 665, 220, '#102433')
        text(x + 18, y + 31, f"{case['id'] + 1:02d}  {case['title']}", 'head')
        text(x + 18, y + 54, f"연속 이정표 2,496단위 · X {case['left']:,.0f}–{case['right']:,.0f}", 'small muted')
        for index, sector in enumerate(case['sectors']):
            sy = y + 66 + index * 49
            text(x + 18, sy + 14, sector['name'], 'small')
            if sector['jump']:
                text(x + 18, sy + 32, '점프·와이어 구간', 'small muted')
            for stop_index, stop in enumerate(sector['stops']):
                bx = x + 155 + stop_index * 143
                color = '#76b5fb' if stop['optional'] else '#f7bc62'
                box(bx, sy, 135, 43, '#172d3e', color, 4)
                title_lines = wrap_title(stop['title'])
                for line_index, line in enumerate(title_lines):
                    text(bx + 6, sy + 13 + line_index * 12, line, 'small')
                text(bx + 6, sy + 38, ('선택 · ' if stop['optional'] else '필수 · ') + actions[stop['action']], 'small muted')
            box(x + 584, sy, 63, 43, '#123d37', '#57d7b2', 4)
            text(x + 591, sy + 16, '주요 목표', 'small')
            text(x + 591, sy + 32, '단서·인계', 'small muted')
    text(40, 1195, '수사로 각 10,400단위 / 일반 구간은 바닥 이동 / 모든 구간 천장 와이어 / 점프 구간은 안전 착지대와 상부 경로', 'body')
    text(40, 1225, '생산구 끝 → 생활부 엘리베이터     생활부 끝 → 조직원전 → 로봇 3페이즈 → 두목 진술 → 행정구 엘리베이터', 'body')
    text(40, 1255, '선택 현장을 건너뛰어도 진행 가능. 완료한 이정표로 돌아와 선택 현장을 해결하고 주민 증언을 다시 확인할 수 있습니다.', 'small muted')
    text(40, 1281, '막히는 시간으로 플레이 분량을 강제하지 않습니다. 150분 목표는 실제 Unity 완주 측정과 조정이 필요합니다.', 'small muted')
    parts.append('</svg>')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text('\n'.join(parts) + '\n', encoding='utf-8')
    print(args.output.resolve())


if __name__ == '__main__':
    main()
