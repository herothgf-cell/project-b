"""제안서 목표 아트 시안 생성 도구.

OpenAI 이미지 API로 프롬프트(와 선택적 참조 이미지)에서 시안 PNG를 만든다.
API 키는 환경 변수 OPENAI_API_KEY에서만 읽는다. 저장소에 키를 넣지 않는다.

사용 예:
  python tools/generate_concept.py --prompt-file docs/proposals/assets/v9/prompts/A-1.txt \
      --ref docs/proposals/assets/v8/04a-single-cockpit.png \
      --out docs/proposals/assets/v9/A-1-after.png

설명: docs/proposals/art-conversion-guide.md
"""
from __future__ import annotations

import argparse
import base64
import os
import sys
from contextlib import ExitStack
from pathlib import Path

STYLE_FILE = Path(__file__).resolve().parent.parent / "docs/proposals/assets/v9/prompts/_style.txt"


def build_prompt(prompt_file: Path, no_style: bool) -> str:
    body = prompt_file.read_text(encoding="utf-8").strip()
    if no_style or not STYLE_FILE.exists():
        return body
    return STYLE_FILE.read_text(encoding="utf-8").strip() + "\n\n" + body


def main() -> int:
    parser = argparse.ArgumentParser(description="목표 아트 시안 생성")
    parser.add_argument("--prompt-file", required=True, type=Path)
    parser.add_argument("--out", required=True, type=Path)
    parser.add_argument("--ref", action="append", type=Path, default=[],
                        help="참조 이미지(여러 번 지정 가능). 지정하면 편집 방식으로 생성")
    parser.add_argument("--model", default="gpt-image-1",
                        help="OpenAI 이미지 모델 이름. 최신 이름은 OpenAI 문서에서 확인")
    parser.add_argument("--size", default="1536x1024", help="가로형 기본값")
    parser.add_argument("--no-style", action="store_true", help="공통 스타일 문구를 붙이지 않음")
    args = parser.parse_args()

    if not os.environ.get("OPENAI_API_KEY"):
        print("OPENAI_API_KEY 환경 변수가 없습니다. 가이드 5-②를 확인하세요.", file=sys.stderr)
        return 2
    try:
        from openai import OpenAI
    except ImportError:
        print("openai 패키지가 없습니다: python -m pip install openai", file=sys.stderr)
        return 2

    prompt = build_prompt(args.prompt_file, args.no_style)
    if args.out.exists():
        print(f"이미 있는 파일은 덮어쓰지 않습니다: {args.out}", file=sys.stderr)
        return 1

    client = OpenAI()
    if args.ref:
        with ExitStack() as stack:
            images = [stack.enter_context(open(p, "rb")) for p in args.ref]
            result = client.images.edit(model=args.model, image=images, prompt=prompt, size=args.size)
    else:
        result = client.images.generate(model=args.model, prompt=prompt, size=args.size)

    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_bytes(base64.b64decode(result.data[0].b64_json))
    log = args.out.parent / "prompts.md"
    refs = ", ".join(str(p).replace("\\", "/") for p in args.ref) or "없음"
    with log.open("a", encoding="utf-8") as f:
        f.write(f"\n## {args.out.name}\n\n모델: {args.model} · 참조: {refs}\n\n```\n{prompt}\n```\n")
    print(f"저장: {args.out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
