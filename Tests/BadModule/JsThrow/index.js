async function badFunction() {
    throw "Oops!... I did it again"
}

exports.handler = async (event, context) => {
    return await badFunction();
}
