"""이전 이름. 기획 문서 HTML 생성은 tools/render_design_docs.py가 맡는다.

    python tools/render_design_index.py  ==  python tools/render_design_docs.py
"""
import os
import runpy
import sys

if __name__ == "__main__":
    sys.argv[0] = os.path.join(os.path.dirname(os.path.abspath(__file__)), "render_design_docs.py")
    runpy.run_path(sys.argv[0], run_name="__main__")
