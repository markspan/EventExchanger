import setuptools

with open("README.md", "r") as fh:
    long_description = fh.read()


setuptools.setup(
    name="pyEVT", 
    version="0.99.6",
    author="Eise Hoekstra and Mark Span (primary developer)",
    author_email="m.m.span@rug.nl",
    description="Package to communicate with RUG developed hardware",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/markspan/pyEVT",
    packages=setuptools.find_packages(),
	include_package_data=True,
	package_data={"pyEVT": ['EventExchanger.dll', 'HidSharp.dll', 'HidSharp.DeviceHelpers.dll']},	
	install_requires=[
          'pythonnet',
      ],
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
		"Intended Audience :: Science/Research",
    ],
    python_requires='>=3.6',
)