# Coding Guidelines

1. a. We will have one protected main branch and one protected dev branch.

b. All client demos would be made from main branch.

c. The dev branch is where all developers will merge into after review.

2. a. Developers should have their branches named as 'dev_<developer_name>'. Developers will have work rights only on their respective branch. When they are ready with a feature, they create pull request to merge into dev.

b. At least two other eyes review the pull request, both developers. In addition, product owners or scrum master can also review if one of them case they can understand the tech stack.

c. Reviewers should be able to build the branch under review, and then check the target functionality, at least locally.

d. Comments are added by reviewers, more changes are made by developer who made the request, iteratively, till all reviewers approve.

3. a. The target branch is then merged into dev.

b. After the dev branch builds and works perfectly on the target platform, with all dependencies like live database, etc., only then it can be merged into main, to make sure main build never fails and main branch is always a functioning demo---Definition of Done.

c. If dev build fails, the merge can be rolled back and the responsible developer should try the fix on original branch. Or if the fail is a result of integration issues on dev branch and not a result of faulty code logic on the branch being merged, it could be solved on dev branch itself.

4. a. Test-driven development (TDD), i.e., writing the tests first (unit tests at least) and then developing the feature could be an approach, but would be best to leave it at respective developer's discretion.

b. However, tests are necessary before committing. It can make reviewing the code easier during merging, as it reveals the intended features in an easy-to-understand way and can also make it easier for reviewer to add their own tests. TDD also makes it clearer for the developers what they want to develop/fix.

5. a. Pull requests should be commented with the functionality implemented (main purpose and maybe short summary of code changes), instructions for installing dependencies, any heads-up for build process.

b. Best to have docstrings for each function (unless when too obvious) and line comments for at least non-intuitive logic.

c. In case, the lack of comments by a developer makes review difficult, reviewers can point that out in comments and ask for more and developers should be mindful of these comments. But no enforcements are necessary right from the beginning.

6. Release Tags & Lightweight Changelogs (To be set-up)

a. What to add: After main is green, create a signed tag like v1.4.0 (Semantic Versioning).

b. Store a CHANGELOG.md that lists features/bug‑fixes in bullet form for each tag.

c. Auto‑generate the changelog from merge commit messages (Conventional Commits + standard‑version, release‑please, etc.).

d. Why it helps: Anyone—devs, QA, or clients—can check out a specific version or roll back quickly, and you get traceable release notes for free

7. Coding freeze for release Tuesday 8 pm

## Instructions in AMOS course:

1. Please configure your name and email address for your local repository

2. Please sign-off on your commits as you work using –-signoff flag

3. If you are pair programming, please make sure you add “Co-authored-by:” to commit message using the correct email address

Follow AMOS B01: Slides 38-45