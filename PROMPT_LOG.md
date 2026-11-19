# Prompt Log

Tool: Claude Code (Claude Opus) in VS Code, unless noted otherwise.

Each entry records the prompt, why I sent it, and what I kept, changed, or rejected from the output.

---

## 1. Planning the build

**Prompt**

> I have this take home assessment and I'm allowed to use AI. Here are the recruiter instructions. You can also look at the take home assessment pdf as well. Please make a detailed plan for a functional app. Make no code changes yet, let me set up the GitHub.

**Why**

I wanted a phased plan before writing any code, so each phase maps to a reviewable set of commits. I also wanted the brief checked for hidden instructions (white text, tiny fonts) since some assessments plant them to catch unreviewed AI output.

**Outcome**

- Kept: the phase breakdown and the layered backend design (provider interface, pure aggregation function, thin route).
- Kept: the AI's live probe of the Yahoo endpoint, which showed the example curl in the brief returns only one day of data. The `range=1mo` parameter is required to meet the "last month" requirement.
- Kept: grouping by the exchange's timezone (not UTC or server time). A naive UTC grouping would still look right for US stocks most of the time, so this is easy to miss.
- Changed: _fill in anything you adjusted in the plan._
- Rejected: _fill in anything you cut._

---

