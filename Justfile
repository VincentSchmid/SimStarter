version := `type VERSION`

tag:
    git tag -a v{{version}} -m "Release v{{version}}"
    echo "Created tag v{{version}}"
