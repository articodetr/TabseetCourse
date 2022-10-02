
function getSelectedCheckboxValues(name) {
    const checkboxes = document.querySelectorAll(`input[name="${name}"]:checked`);
    let values = [];
    checkboxes.forEach((checkbox) => {
        values.push(checkbox.value);
    });
    return values;
}
function getSelectedRadioBtnBaluss(name) {
    //var selectedGender = new Array();
    //var genders = document.getElementsByName(name);


    //for (var i = 0; i < genders.length; i++) {
    //    if (genders[i].checked)
    //        selectedGender.push(genders[i].value);
    //}
    // selectedGender;
    return $(`input[name="${name}"]:checked`).val();


}

function SplitStringToArry(string) {
    let values = [];
    var result = string.split();
    for (var i = 0; i < result.length; i++) {
        values.push(result.value);
    }
    return values;
}

function numberToEnglish(s) {
    var th_val = ['', 'thousand', 'million', 'billion', 'trillion'];
    // System for uncomment this line for Number of English 
    // var th_val = ['','thousand','million', 'milliard','billion'];

    var dg_val = ['zero', 'one', 'two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine'];
    var tn_val = ['ten', 'eleven', 'twelve', 'thirteen', 'fourteen', 'fifteen', 'sixteen', 'seventeen', 'eighteen', 'nineteen'];
    var tw_val = ['twenty', 'thirty', 'forty', 'fifty', 'sixty', 'seventy', 'eighty', 'ninety'];
    s = s.toString();
    s = s.replace(/[\, ]/g, '');
    if (s != parseFloat(s))
        return 'not a number ';
    var x_val = s.indexOf('.');
    if (x_val == -1)
        x_val = s.length;
    if (x_val > 15)
        return 'too big';
    var n_val = s.split('');
    var str_val = '';
    var sk_val = 0;
    for (var i = 0; i < x_val; i++) {
        if ((x_val - i) % 3 == 2) {
            if (n_val[i] == '1') {
                str_val += tn_val[Number(n_val[i + 1])] + ' ';
                i++;
                sk_val = 1;
            } else if (n_val[i] != 0) {
                str_val += tw_val[n_val[i] - 2] + ' ';
                sk_val = 1;
            }
        } else if (n_val[i] != 0) {
            str_val += dg_val[n_val[i]] + ' ';
            if ((x_val - i) % 3 == 0)
                str_val += 'hundred ';
            sk_val = 1;
        }
        if ((x_val - i) % 3 == 1) {
            if (sk_val)
                str_val += th_val[(x_val - i - 1) / 3] + ' ';
            sk_val = 0;
        }
    }
    if (x_val != s.length) {
        var y_val = s.length;
        str_val += 'point ';
        for (var i = x_val + 1; i < y_val; i++)
            str_val += dg_val[n_val[i]] + ' ';
    }
    $('#totaoInWord').html(str_val + ' USD');
    return str_val.replace(/\s+/g, ' ');



}


//////Generate Rundom Number

function getRandomNumber(min, max) {
    let step1 = max - min + 1;
    let step2 = Math.random() * step1;
    let result = Math.floor(step2) + min;
    return result;
}
function createArrayOfNumbers(start, end) {
    let myArray = [];
    for (let i = start; i <= end; i++) {
        myArray.push(i);
    }
    return myArray;
}

function GenerateRundomNumberBetweenRange(numbersArray) {

    if (numbersArray.length == 0) {

        return 0;
    }
    let randomIndex = getRandomNumber(0, numbersArray.length - 1);
    let randomNumber = numbersArray[randomIndex];
    numbersArray.splice(randomIndex, 1)
    return randomNumber;
};
//////////end rand dom number
function SaveToLocalHost(key, value) {
    var L_product = [];
    L_product.push(value);
    var basketProduct = localStorage.getItem(key);
    var obj = JSON.parse(basketProduct);
    if (obj != null) {
        for (var i = 0; i < obj.length; i++) {
            L_product.push(obj[i]); 
        }
    }
  
     localStorage.setItem(key, JSON.stringify(L_product));
 
    
  
   
}

